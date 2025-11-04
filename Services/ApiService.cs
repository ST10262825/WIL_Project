using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using TutorConnect.WebApp.Models;
using static System.Net.WebRequestMethods;


namespace TutorConnect.WebApp.Services
{
    public class ApiService
    {
        private readonly HttpClient _client;
        private readonly IHttpContextAccessor _httpContextAccessor;


        public ApiService(IHttpClientFactory clientFactory, IHttpContextAccessor httpContextAccessor)
        {
            _client = clientFactory.CreateClient("TutorConnectAPI");
            _httpContextAccessor = httpContextAccessor;
        }

        private void AddAuthHeader()
        {
            var user = _httpContextAccessor.HttpContext?.User;

            if (user == null) return;

            var token = user.FindFirst("Token")?.Value;
            var jwtExpiresStr = user.FindFirst("JWT_Expires")?.Value;

            // Check if JWT has expired
            if (!string.IsNullOrEmpty(jwtExpiresStr) &&
                DateTime.TryParse(jwtExpiresStr, null, System.Globalization.DateTimeStyles.RoundtripKind, out var jwtExpires) &&
                jwtExpires < DateTime.UtcNow)
            {
                token = null; // token expired
            }

            if (!string.IsNullOrWhiteSpace(token))
            {
                if (_client.DefaultRequestHeaders.Contains("Authorization"))
                    _client.DefaultRequestHeaders.Remove("Authorization");

                _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
            else
            {
                _client.DefaultRequestHeaders.Authorization = null;
            }
        }

        // ==========================
        // Booking-related methods
        // ==========================

        // Create booking (supports start/end time)
        public async Task<bool> CreateBookingAsync(CreateBookingDTO dto)
        {
            var json = System.Text.Json.JsonSerializer.Serialize(dto);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _client.PostAsync("api/bookings/create", content);
            return response.IsSuccessStatusCode;
        }

        // ==========================
        // Chat-related methods
        // ==========================

 
        public async Task<int> GetUnreadCountAsync()
        {
            AddAuthHeader();
            try
            {
                var userIdClaim = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    Console.WriteLine("[ApiService] GetUnreadCountAsync: UserId claim missing");
                    return 0;
                }

                Console.WriteLine($"[ApiService] GetUnreadCountAsync: Getting unread count for user {userId}");

                var response = await _client.GetAsync($"api/chat/unread-count");

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"[ApiService] GetUnreadCountAsync: Error - {response.StatusCode}");
                    return 0;
                }

                var result = await response.Content.ReadFromJsonAsync<dynamic>();
                int unreadCount = result?.unreadCount ?? 0;

                Console.WriteLine($"[ApiService] GetUnreadCountAsync: Unread count = {unreadCount}");
                return unreadCount;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ApiService] GetUnreadCountAsync: Exception - {ex.Message}");
                return 0;
            }
        }

        


        // Get tutor bookings
        public async Task<List<BookingDTO>> GetTutorBookingsAsync(int tutorId)
        {
            var response = await _client.GetAsync($"api/bookings/tutor/{tutorId}");
            if (!response.IsSuccessStatusCode)
                return new List<BookingDTO>();

            var json = await response.Content.ReadAsStringAsync();
            return System.Text.Json.JsonSerializer.Deserialize<List<BookingDTO>>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? new List<BookingDTO>();
        }

        // Get available slots for a tutor on a specific date
        public async Task<List<TimeSlotDTO>> GetTutorAvailabilityAsync(int tutorId, DateTime date)
        {
            var url = $"api/bookings/tutor/{tutorId}/availability?date={date:yyyy-MM-dd}";
            Console.WriteLine($"Calling API: {url}");

            var response = await _client.GetAsync(url);

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"API Response: {content}");

                try
                {
                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    };
                    return System.Text.Json.JsonSerializer.Deserialize<List<TimeSlotDTO>>(content, options);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Deserialization error: {ex.Message}");
                    return new List<TimeSlotDTO>();
                }
            }
            else
            {
                Console.WriteLine($"API Error: {response.StatusCode}");
                return new List<TimeSlotDTO>();
            }
        }

        // Update booking status
        public async Task<bool> UpdateBookingStatusAsync(int bookingId, string status)
        {
            var response = await _client.PutAsync($"api/bookings/update-status/{bookingId}?status={status}", null);
            return response.IsSuccessStatusCode;
        }







        public async Task<StudentDTO> GetStudentByUserIdAsync()
        {
            try
            {
                AddAuthHeader();

                var userIdClaim = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                    return null;

                var response = await _client.GetAsync($"api/student/by-user/{userId}");

                if (!response.IsSuccessStatusCode)
                    return null;

                var content = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<StudentDTO>(content);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting student by user ID: {ex.Message}");
                return null;
            }
        }

        public async Task<List<BookingDTO>> GetStudentBookingsAsync(int studentId)
        {
            try
            {
                AddAuthHeader();
                var response = await _client.GetAsync($"api/student/{studentId}/bookings");

                if (!response.IsSuccessStatusCode)
                    return new List<BookingDTO>();

                var content = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<List<BookingDTO>>(content) ?? new List<BookingDTO>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting student bookings: {ex.Message}");
                return new List<BookingDTO>();
            }
        }

        public async Task<StudentDashboardSummaryDTO> GetStudentDashboardSummaryAsync()
        {
            try
            {
                AddAuthHeader();
                var student = await GetStudentByUserIdAsync();
                if (student == null) return null;

                var response = await _client.GetAsync($"api/student/{student.StudentId}/dashboard-summary");
                if (!response.IsSuccessStatusCode)
                    return null;

                return await response.Content.ReadFromJsonAsync<StudentDashboardSummaryDTO>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting dashboard summary: {ex.Message}");
                return null;
            }
        }

        public async Task<(bool Success, string ProfileImageUrl, string Message)> UpdateStudentProfileAsync(int studentId, string bio, IFormFile profileImage)
        {
            AddAuthHeader();
            try
            {
                using var form = new MultipartFormDataContent();

                if (!string.IsNullOrEmpty(bio))
                {
                    form.Add(new StringContent(bio), "Bio");
                }

                if (profileImage != null)
                {
                    var streamContent = new StreamContent(profileImage.OpenReadStream());
                    form.Add(streamContent, "ProfileImage", profileImage.FileName);
                }

                var response = await _client.PutAsync($"api/student/{studentId}/profile", form);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var result = JsonConvert.DeserializeObject<dynamic>(content);

                    bool success = result?.success ?? false;
                    string message = result?.message?.ToString();
                    string imageUrl = result?.profileImageUrl?.ToString();

                    return (success, imageUrl, message);
                }

                return (false, null, "Failed to update profile");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating student profile: {ex.Message}");
                return (false, null, "Error updating profile");
            }
        }

        public async Task<List<BookingDTO>> GetUpcomingSessionsAsync(int studentId)
        {
            AddAuthHeader();
            try
            {
                var response = await _client.GetAsync($"api/student/{studentId}/upcoming-sessions");
                if (!response.IsSuccessStatusCode)
                    return new List<BookingDTO>();

                return await response.Content.ReadFromJsonAsync<List<BookingDTO>>() ?? new List<BookingDTO>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting upcoming sessions: {ex.Message}");
                return new List<BookingDTO>();
            }
        }




        private async Task<T> HandleResponse<T>(HttpResponseMessage response)
        {
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException($"API Error ({response.StatusCode}): {error}");
            }

            return await response.Content.ReadFromJsonAsync<T>();
        }

        // Admin-related (secured)

        public async Task<Dictionary<string, int>> GetAdminAnalyticsAsync()
        {
            AddAuthHeader();
            var response = await _client.GetAsync("api/admin/analytics");
            return await HandleResponse<Dictionary<string, int>>(response);
        }

        public async Task<List<TutorDTO>> GetTutorsAsync()
        {
            AddAuthHeader();
            var response = await _client.GetAsync("api/admin/tutors");
            return await HandleResponse<List<TutorDTO>>(response);
        }

        public async Task<TutorDTO> GetTutorByIdAsync(int tutorId)
        {
            AddAuthHeader();
            var response = await _client.GetAsync($"api/tutor-dashboard/by-id/{tutorId}");
            return await HandleResponse<TutorDTO>(response);
        }


        public async Task<bool> CreateTutorAsync(CreateTutorDTO dto)
        {
            AddAuthHeader();
            var response = await _client.PostAsJsonAsync("api/admin/create-tutor", dto);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> UpdateTutorAsync(UpdateTutorDTO dto)
        {
            AddAuthHeader();
            var response = await _client.PutAsJsonAsync($"api/admin/update-tutor/{dto.Id}", dto);
            return response.IsSuccessStatusCode;
        }




        public async Task<(bool Success, string Message, bool HasPendingBookings)> DeleteTutorAsync(int id)
        {
            AddAuthHeader();
            try
            {
                Console.WriteLine($"API Service: Deleting tutor {id}");
                var response = await _client.DeleteAsync($"api/admin/delete-tutor/{id}");

                Console.WriteLine($"API Response Status: {response.StatusCode}");

                var responseContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"API Response Content: {responseContent}");

                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine("Delete successful");
                    return (true, "Tutor deleted successfully.", false);
                }

                // Check for specific error conditions
                bool hasPendingBookings = responseContent.Contains("pending bookings", StringComparison.OrdinalIgnoreCase);
                bool hasActiveBookings = responseContent.Contains("active bookings", StringComparison.OrdinalIgnoreCase);

                string errorMessage = hasPendingBookings ?
                    "Cannot delete tutor with pending bookings. Please resolve pending bookings first." :
                    hasActiveBookings ?
                    "Cannot delete tutor with active upcoming sessions. Please resolve all bookings first." :
                    $"Delete failed: {responseContent}";

                Console.WriteLine($"Delete failed: {errorMessage}");
                return (false, errorMessage, hasPendingBookings);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"API Service Exception: {ex.Message}");
                return (false, $"An error occurred: {ex.Message}", false);
            }
        }

        public async Task<TutorDTO> GetTutorProfileAsync(int tutorId)
        {
            AddAuthHeader();
            var response = await _client.GetAsync($"api/admin/tutors/{tutorId}");
            return await HandleResponse<TutorDTO>(response);
        }


        public async Task<bool> ToggleBlockTutorAsync(int id)
        {
            AddAuthHeader();
            // Fetch current status
            var tutor = await GetTutorByIdAsync(id);
            string action = tutor.IsBlocked ? "unblock-tutor" : "block-tutor";

            var response = await _client.PutAsync($"api/admin/{action}/{id}", null);
            return response.IsSuccessStatusCode;
        }

        public async Task<List<StudentDTO>> GetStudentsAsync()
        {
            AddAuthHeader();
            var response = await _client.GetAsync("api/admin/students");
            return await HandleResponse<List<StudentDTO>>(response);
        }

        // Public endpoints

        public async Task<string> LoginAsync(LoginDTO loginDto)
        {
            try
            {
                var response = await _client.PostAsJsonAsync("api/auth/login", loginDto);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();

                    // Check for blocked account message
                    if (errorContent.Contains("Account currently blocked", StringComparison.OrdinalIgnoreCase))
                    {
                        throw new HttpRequestException("Account currently blocked. Please contact your Administrator.");
                    }

                    // Check for email not verified
                    if (errorContent.Contains("Email not verified", StringComparison.OrdinalIgnoreCase))
                    {
                        throw new HttpRequestException("Email not verified");
                    }

                    throw new HttpRequestException($"Login failed: {errorContent}");
                }

                var json = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();
                return json["token"];
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Login error: {ex.Message}");
                throw;
            }
        }




        public async Task<bool> IsUserBlockedAsync(int userId)
        {
            AddAuthHeader();
            try
            {
                var response = await _client.GetAsync($"api/auth/user/{userId}/blocked-status");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    return JsonConvert.DeserializeObject<bool>(content);
                }
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error checking blocked status: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> RegisterStudentAsync(RegisterStudentDTO dto)
        {
            var response = await _client.PostAsJsonAsync("api/auth/student-register", dto);
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException($"Registration failed: {error}");
            }

            return true;
        }

        public async Task<List<ModuleDTO>> GetModulesAsync()
        {
            var response = await _client.GetAsync("api/module");
            return await HandleResponse<List<ModuleDTO>>(response);
        }

        public async Task<bool> VerifyTokenAsync(string email, string token)
        {
            var response = await _client.PostAsJsonAsync("api/auth/verify-token", new
            {
                Email = email,
                Token = token
            });

            return response.IsSuccessStatusCode;
        }

        public async Task<bool> VerifyEmailAsync(string token)
        {
            var response = await _client.GetAsync($"api/auth/verify-email?token={token}");
            return response.IsSuccessStatusCode;
        }



        public async Task<StudentDTO> GetStudentByUserIdAsync(int? userId = null)
        {
            AddAuthHeader();

            try
            {
                int actualUserId;
                if (userId.HasValue)
                {
                    actualUserId = userId.Value;
                }
                else
                {
                    // Get UserId from the logged-in user claims
                    var userIdClaim = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                    if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out actualUserId))
                        throw new HttpRequestException("UserId claim missing. Please re-login.");
                }

                // Call the API endpoint - you need to implement this in your API
                var response = await _client.GetAsync($"api/student/by-user/{actualUserId}");

                if (!response.IsSuccessStatusCode)
                    return null;

                var content = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<StudentDTO>(content);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting student by user ID: {ex.Message}");
                return null;
            }
        }

        // Add this method to your ApiService
        public string GetProfileImageUrl(int studentId)
        {
            // Use the API endpoint to serve images through the controller
            return $"{_client.BaseAddress}api/student/{studentId}/profile-image";
        }

        // Also add this method for tutor profile images
        public string GetTutorProfileImageUrl(int tutorId)
        {
            return $"{_client.BaseAddress}api/tutor/{tutorId}/profile-image";
        }

        public async Task<bool> UpdateSessionStatusAsync(int sessionId, string newStatus, string? reason = null)
        {
            var payload = new
            {
                Status = newStatus,
                RejectionReason = reason
            };

            var response = await _client.PutAsJsonAsync($"api/tutor-dashboard/sessions/{sessionId}/status", payload);
            return response.IsSuccessStatusCode;
        }

        public async Task<TutorDTO> GetTutorByUserIdAsync(int userId)
        {
            try
            {
                Console.WriteLine($"[ApiService] Getting tutor by user ID: {userId}");
                var response = await _client.GetAsync($"api/tutor-dashboard/user/{userId}");

                if (!response.IsSuccessStatusCode)
                {
                    if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                    {
                        Console.WriteLine($"[ApiService] Tutor not found for user ID: {userId}");
                        return null; // Return null instead of throwing exception
                    }

                    var errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"[ApiService] API error: {response.StatusCode} - {errorContent}");
                    throw new HttpRequestException($"API returned error: {response.StatusCode} - {errorContent}");
                }

                var tutor = await response.Content.ReadFromJsonAsync<TutorDTO>();
                Console.WriteLine($"[ApiService] Successfully retrieved tutor: {tutor?.Name}");
                return tutor;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ApiService] Exception getting tutor by user ID: {ex.Message}");
                throw;
            }
        }



        public async Task<TutorDTO> GetAdminTutorByIdAsync(int tutorId)
        {
            AddAuthHeader();
            try
            {
                var response = await _client.GetAsync($"api/admin/tutors/{tutorId}");

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"Failed to get tutor: {response.StatusCode}");
                    return null;
                }

                var content = await response.Content.ReadAsStringAsync();
                var tutor = JsonConvert.DeserializeObject<TutorDTO>(content);
                return tutor;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting tutor by ID: {ex.Message}");
                return null;
            }
        }



        public async Task<HttpResponseMessage> ApproveSessionAsync(int sessionId)
        {
            return await _client.PutAsync($"api/tutor-dashboard/sessions/{sessionId}/approve", null);
        }

        public async Task<HttpResponseMessage> RejectSessionAsync(int sessionId, string reason)
        {
            var content = JsonContent.Create(reason);
            return await _client.PutAsync($"api/tutor-dashboard/sessions/{sessionId}/reject", content);
        }


        public async Task<List<MessageDTO>> GetChatHistoryAsync(int otherUserId)
        {
            AddAuthHeader();

            var userIdClaim = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out int userId))
                throw new HttpRequestException("UserId claim missing.");

            var response = await _client.GetAsync($"api/chat/history/{userId}/{otherUserId}");
            if (!response.IsSuccessStatusCode)
                return new List<MessageDTO>();

            var json = await response.Content.ReadAsStringAsync();
            return System.Text.Json.JsonSerializer.Deserialize<List<MessageDTO>>(json,
                new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new List<MessageDTO>();
        }

        public async Task<List<ChatUserDTO>> GetChatUsersAsync()
        {
            try
            {
                AddAuthHeader();
                Console.WriteLine("ApiService: Fetching chat users from API...");

                var response = await _client.GetAsync("api/chat/users");
                Console.WriteLine($"ApiService: Response status: {response.StatusCode}");

                if (response.IsSuccessStatusCode)
                {
                    var users = await response.Content.ReadFromJsonAsync<List<ChatUserDTO>>();
                    Console.WriteLine($"ApiService: Retrieved {users?.Count ?? 0} users");
                    return users ?? new List<ChatUserDTO>();
                }
                else
                {
                    Console.WriteLine($"ApiService: Error - {response.StatusCode}");
                    return new List<ChatUserDTO>();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ApiService: Exception - {ex.Message}");
                return new List<ChatUserDTO>();
            }
        }


        public async Task<(bool IsSuccess, string ErrorMessage, string ProfileImageUrl)>
    UpdateTutorProfileAsync(int tutorId, string bio, string aboutMe, string expertise, string education, IFormFile profileImage)
        {
            AddAuthHeader();
            try
            {
                Console.WriteLine($"[API SERVICE] UpdateTutorProfileAsync START");
                Console.WriteLine($"[API SERVICE] TutorId: {tutorId}");
                Console.WriteLine($"[API SERVICE] Bio: {bio}");
                Console.WriteLine($"[API SERVICE] AboutMe: {aboutMe}");
                Console.WriteLine($"[API SERVICE] Expertise: {expertise}");
                Console.WriteLine($"[API SERVICE] Education: {education}");
                Console.WriteLine($"[API SERVICE] ProfileImage: {profileImage?.FileName ?? "null"}");

                using var formData = new MultipartFormDataContent();

                formData.Add(new StringContent(bio ?? ""), "Bio");
                formData.Add(new StringContent(aboutMe ?? ""), "AboutMe");
                formData.Add(new StringContent(expertise ?? ""), "Expertise");
                formData.Add(new StringContent(education ?? ""), "Education");

                if (profileImage != null)
                {
                    Console.WriteLine($"[API SERVICE] Adding profile image: {profileImage.FileName}");
                    var fileContent = new StreamContent(profileImage.OpenReadStream());
                    fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(profileImage.ContentType);
                    formData.Add(fileContent, "ProfileImage", profileImage.FileName);
                }

                Console.WriteLine($"[API SERVICE] Sending PUT request to: api/tutor-dashboard/{tutorId}/profile");
                var response = await _client.PutAsync($"api/tutor-dashboard/{tutorId}/profile", formData);

                Console.WriteLine($"[API SERVICE] Response status: {response.StatusCode}");

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"[API SERVICE] Error response: {errorContent}");
                    return (false, errorContent, null);
                }

                var json = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();
                string updatedUrl = null;

                if (json != null && json.TryGetValue("profileImageUrl", out var url) && !string.IsNullOrEmpty(url))
                {
                    updatedUrl = Uri.IsWellFormedUriString(url, UriKind.Absolute) ? url : new Uri(_client.BaseAddress!, url).ToString();
                }

                Console.WriteLine($"[API SERVICE] Update successful, ProfileImageUrl: {updatedUrl ?? "null"}");
                return (true, null, updatedUrl);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[API SERVICE] Exception: {ex.Message}");
                Console.WriteLine($"[API SERVICE] Stack: {ex.StackTrace}");
                return (false, ex.Message, null);
            }
        }


        public async Task<(bool Success, string Message)> DeleteStudentAsync(int id)
        {
            AddAuthHeader();
            try
            {
                Console.WriteLine($"API Service: Deleting student {id}");
                var response = await _client.DeleteAsync($"api/admin/delete-student/{id}");

                Console.WriteLine($"API Response Status: {response.StatusCode}");

                var responseContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"API Response Content: {responseContent}");

                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine("Delete successful");
                    return (true, "Student deleted successfully.");
                }

                // Handle specific error conditions
                if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
                {
                    try
                    {
                        var errorObj = JsonConvert.DeserializeObject<dynamic>(responseContent);
                        string errorMessage = errorObj?.message?.ToString() ?? responseContent;

                        bool hasGamificationProfile = errorObj?.hasGamificationProfile?.Value ?? false;
                        bool hasActiveBookings = errorObj?.hasActiveBookings?.Value ?? false;
                        bool hasReviews = errorObj?.hasReviews?.Value ?? false;

                        return (false, errorMessage);
                    }
                    catch
                    {
                        return (false, responseContent);
                    }
                }

                return (false, $"Delete failed: {responseContent}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"API Service Exception: {ex.Message}");
                return (false, $"An error occurred: {ex.Message}");
            }
        }


        public async Task<T> GetAsync<T>(string url)
        {
            var response = await _client.GetAsync(url);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<T>();
        }

        public async Task<int> GetUnreadMessagesCountAsync(int userId)
        {
            AddAuthHeader();
            try
            {
                // Ensure the URL matches your actual API route
                var response = await _client.GetAsync($"api/chat/unread-count/{userId}");

                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<int>();
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    Console.WriteLine($"[ApiService] Unauthorized accessing chat endpoint for user {userId}");
                    return 0; // Return 0 instead of throwing exception
                }
                else
                {
                    Console.WriteLine($"[ApiService] Failed to get unread count: {response.StatusCode}");
                    return 0;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ApiService] Error getting unread count: {ex.Message}");
                return 0; // Return 0 instead of crashing
            }
        }


        public async Task<bool> MarkMessagesAsReadAsync(int senderId)
        {
            AddAuthHeader();
            var response = await _client.PutAsync($"api/chat/mark-read/{senderId}", null);
            return response.IsSuccessStatusCode;
        }

        public async Task<List<ChatUserDTO>> GetChatContactsAsync()
        {
            AddAuthHeader();
            var response = await _client.GetAsync("api/chat/contacts");
            return await HandleResponse<List<ChatUserDTO>>(response);
        }



       

        public async Task<List<BrowseTutorDTO>> GetAllTutorsAsync()
        {
            var response = await _client.GetAsync("api/tutor-dashboard/browse");
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<List<BrowseTutorDTO>>(json) ?? new List<BrowseTutorDTO>();
        }


        public async Task<List<TutorDTO>> GetAllAdminTutorsAsync()
        {
            AddAuthHeader();
            try
            {
                // Use the correct admin endpoint
                var response = await _client.GetAsync("api/admin/tutors");

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"API Error: {response.StatusCode}");
                    return new List<TutorDTO>();
                }

                var content = await response.Content.ReadAsStringAsync();
                var tutors = JsonConvert.DeserializeObject<List<TutorDTO>>(content) ?? new List<TutorDTO>();

                // For now, set default values for missing properties
                foreach (var tutor in tutors)
                {
                    tutor.TotalBookings = tutor.TotalBookings; // Will be 0 from API, but that's okay
                    tutor.AverageRating = tutor.AverageRating; // Will be 0 from API
                }

                return tutors;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting tutors: {ex.Message}");
                return new List<TutorDTO>();
            }
        }


        public async Task<List<PendingReviewDTO>> GetPendingReviewsAsync(int studentId)
        {
            AddAuthHeader();
            try
            {
                var response = await _client.GetAsync($"api/reviews/student/{studentId}/pending");
                if (!response.IsSuccessStatusCode)
                    return new List<PendingReviewDTO>();

                return await response.Content.ReadFromJsonAsync<List<PendingReviewDTO>>() ?? new List<PendingReviewDTO>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting pending reviews: {ex.Message}");
                return new List<PendingReviewDTO>();
            }
        }

        public async Task<bool> SubmitReviewAsync(CreateReviewDTO review)
        {
            AddAuthHeader();
            try
            {
                var response = await _client.PostAsJsonAsync("api/reviews", review);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error submitting review: {ex.Message}");
                return false;
            }
        }

        public async Task<List<ReviewDTO>> GetTutorReviewsAsync(int tutorId)
        {
            try
            {
                var response = await _client.GetAsync($"api/reviews/tutor/{tutorId}");
                if (!response.IsSuccessStatusCode)
                    return new List<ReviewDTO>();

                return await response.Content.ReadFromJsonAsync<List<ReviewDTO>>() ?? new List<ReviewDTO>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting tutor reviews: {ex.Message}");
                return new List<ReviewDTO>();
            }
        }

        public async Task<TutorRatingDTO> GetTutorRatingsAsync(int tutorId)
        {
            try
            {
                var response = await _client.GetAsync($"api/tutors/{tutorId}/ratings");
                if (!response.IsSuccessStatusCode)
                    return new TutorRatingDTO();

                return await response.Content.ReadFromJsonAsync<TutorRatingDTO>() ?? new TutorRatingDTO();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting tutor ratings: {ex.Message}");
                return new TutorRatingDTO();
            }
        }


        public async Task<bool> ChangeTutorPasswordAsync(ChangePasswordDTO dto)
        {
            AddAuthHeader();
            try
            {
                Console.WriteLine($"[WebApp] Changing tutor password...");

                var response = await _client.PutAsJsonAsync("api/tutor/change-password", dto);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new HttpRequestException(errorContent);
                }

                Console.WriteLine($"[WebApp] ✅ Tutor password changed successfully");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[WebApp] ❌ Error changing tutor password: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> ForgotPasswordAsync(string email)
        {
            try
            {
                var response = await _client.PostAsJsonAsync("api/auth/forgot-password", new { Email = email });

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new HttpRequestException(errorContent);
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in forgot password: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> ResetPasswordAsync(string token, string newPassword, string confirmPassword)
        {
            try
            {
                var dto = new ResetPasswordDTO
                {
                    Token = token,
                    NewPassword = newPassword,
                    ConfirmPassword = confirmPassword
                };

                var response = await _client.PostAsJsonAsync("api/auth/reset-password", dto);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new HttpRequestException(errorContent);
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error resetting password: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> ValidateResetTokenAsync(string token)
        {
            try
            {
                var response = await _client.PostAsJsonAsync("api/auth/validate-reset-token", new { Token = token });
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error validating reset token: {ex.Message}");
                return false;
            }
        }


        public async Task<ConsentStatusDTO> GetConsentStatusAsync()
        {
            AddAuthHeader();
            try
            {
                var response = await _client.GetAsync("api/auth/consent-status");
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<ConsentStatusDTO>();
                }
                return new ConsentStatusDTO();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting consent status: {ex.Message}");
                return new ConsentStatusDTO();
            }
        }

        public async Task<bool> UpdateConsentAsync(UpdateConsentDTO dto)
        {
            AddAuthHeader();
            try
            {
                var response = await _client.PostAsJsonAsync("api/auth/update-consent", dto);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating consent: {ex.Message}");
                throw;
            }
        }

        public async Task<dynamic> RequestDataExportAsync()
        {
            AddAuthHeader();
            try
            {
                var response = await _client.PostAsync("api/auth/request-data-export", null);
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<dynamic>();
                }
                throw new HttpRequestException("Failed to request data export");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error requesting data export: {ex.Message}");
                throw;
            }
        }

        public async Task<dynamic> RequestAccountDeletionAsync(DeletionRequestDTO dto)
        {
            AddAuthHeader();
            try
            {
                var response = await _client.PostAsJsonAsync("api/auth/request-account-deletion", dto);
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<dynamic>();
                }
                throw new HttpRequestException("Failed to request account deletion");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error requesting account deletion: {ex.Message}");
                throw;
            }
        }


        // Create a small class to map the API response
        public class ReviewedResponse
        {
            public bool HasBeenReviewed { get; set; }
        }

        // In your ApiService
        public async Task<bool> HasBookingBeenReviewedAsync(int bookingId)
        {
            AddAuthHeader();
            try
            {
                var response = await _client.GetAsync($"api/reviews/booking/{bookingId}/reviewed");
                if (!response.IsSuccessStatusCode)
                    return false;

                var result = await response.Content.ReadFromJsonAsync<ReviewedResponse>();
                return result?.HasBeenReviewed ?? false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error checking if booking has been reviewed: {ex.Message}");
                return false;
            }
        }





        public async Task<T> PostAsync<T>(string endpoint, object data)
        {
            AddAuthHeader();
            var response = await _client.PostAsJsonAsync(endpoint, data);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<T>();
        }

        public async Task DeleteAsync(string endpoint)
        {
            AddAuthHeader();
            var response = await _client.DeleteAsync(endpoint);
            response.EnsureSuccessStatusCode();
        }







        // Update tutor method
        public async Task<bool> AdminUpdateTutorAsync(AdminUpdateTutorDTO dto)
        {
            AddAuthHeader();
            try
            {
                var response = await _client.PutAsJsonAsync($"api/admin/update-tutor/{dto.Id}", dto);

                if (response.IsSuccessStatusCode)
                {
                    return true;
                }

                // Handle specific error cases
                if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Update failed: {errorContent}");
                    throw new HttpRequestException($"Update failed: {errorContent}");
                }

                response.EnsureSuccessStatusCode();
                return true;
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"HTTP error updating tutor: {ex.Message}");
                throw;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating tutor: {ex.Message}");
                throw new HttpRequestException($"Error updating tutor: {ex.Message}", ex);
            }
        }



        // In your ApiService.cs

        public async Task<List<BookingDTO>> GetAllBookingsAsync()
        {
            AddAuthHeader();
            try
            {
                var response = await _client.GetAsync("api/admin/bookings");

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"Failed to get bookings: {response.StatusCode}");
                    return new List<BookingDTO>();
                }

                var content = await response.Content.ReadAsStringAsync();
                var bookings = JsonConvert.DeserializeObject<List<BookingDTO>>(content);
                return bookings ?? new List<BookingDTO>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting bookings: {ex.Message}");
                return new List<BookingDTO>();
            }
        }

        public async Task<bool> DeleteBookingAsync(int id)
        {
            AddAuthHeader();
            try
            {
                var response = await _client.DeleteAsync($"api/admin/delete-booking/{id}");

                if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    if (errorContent.Contains("pending", StringComparison.OrdinalIgnoreCase))
                    {
                        return false;
                    }
                }

                response.EnsureSuccessStatusCode();
                return true;
            }
            catch (HttpRequestException ex) when (ex.Message.Contains("pending", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting booking: {ex.Message}");
                return false;
            }
        }

        public async Task<List<ModuleDTO>> GetAllModulesAsync()
        {
            AddAuthHeader();
            try
            {
                var response = await _client.GetAsync("api/admin/modules");
                if (!response.IsSuccessStatusCode)
                    return new List<ModuleDTO>();

                var content = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<List<ModuleDTO>>(content) ?? new List<ModuleDTO>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting modules: {ex.Message}");
                return new List<ModuleDTO>();
            }
        }

        public async Task<(bool Success, string Message)> CreateModuleAsync(CreateModuleRequest request)
        {
            AddAuthHeader();
            try
            {
                // The request should now include CourseId
                var response = await _client.PostAsJsonAsync("api/admin/create-module", request);

                if (response.IsSuccessStatusCode)
                {
                    return (true, "Module created successfully.");
                }

                var errorContent = await response.Content.ReadAsStringAsync();
                return (false, errorContent);
            }
            catch (Exception ex)
            {
                return (false, $"Error creating module: {ex.Message}");
            }
        }

        public async Task<(bool Success, string Message)> UpdateModuleAsync(int id, UpdateModuleRequest request)
        {
            AddAuthHeader();
            try
            {
                // The request should now include CourseId
                var response = await _client.PutAsJsonAsync($"api/admin/update-module/{id}", request);

                if (response.IsSuccessStatusCode)
                {
                    return (true, "Module updated successfully.");
                }

                var errorContent = await response.Content.ReadAsStringAsync();
                return (false, errorContent);
            }
            catch (Exception ex)
            {
                return (false, $"Error updating module: {ex.Message}");
            }
        }

        public async Task<(bool Success, string Message)> DeleteModuleAsync(int id)
        {
            AddAuthHeader();
            try
            {
                var response = await _client.DeleteAsync($"api/admin/delete-module/{id}");

                if (response.IsSuccessStatusCode)
                {
                    return (true, "Module deleted successfully.");
                }

                var errorContent = await response.Content.ReadAsStringAsync();
                return (false, errorContent);
            }
            catch (Exception ex)
            {
                return (false, $"Error deleting module: {ex.Message}");
            }
        }



        public async Task<List<StudentDTO>> GetAllStudentsAsync()
        {
            AddAuthHeader();
            try
            {
                var resp = await _client.GetAsync("api/admin/students");
                if (!resp.IsSuccessStatusCode)
                    return new List<StudentDTO>();

                var content = await resp.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<List<StudentDTO>>(content)
                       ?? new List<StudentDTO>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"GetAllStudentsAsync error: {ex.Message}");
                return new List<StudentDTO>();
            }
        }

        // In your ApiService.cs

        public async Task<bool> BlockStudentAsync(int id)
        {
            AddAuthHeader();
            try
            {
                var response = await _client.PutAsync($"api/admin/block-student/{id}", null);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error blocking student: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> UnblockStudentAsync(int id)
        {
            AddAuthHeader();
            try
            {
                var response = await _client.PutAsync($"api/admin/unblock-student/{id}", null);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error unblocking student: {ex.Message}");
                return false;
            }
        }

       

        // -- Tutors (convenience wrappers; you already have ToggleBlockTutorAsync) --
        public async Task<bool> BlockTutorAsync(int id)
        {
            AddAuthHeader();
            try
            {
                var resp = await _client.PutAsync($"api/admin/block-tutor/{id}", null);
                return resp.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"BlockTutorAsync error: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> UnblockTutorAsync(int id)
        {
            AddAuthHeader();
            try
            {
                var resp = await _client.PutAsync($"api/admin/unblock-tutor/{id}", null);
                return resp.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"UnblockTutorAsync error: {ex.Message}");
                return false;
            }
        }


        // Add this method to your ApiService
        public async Task<List<BrowseTutorDTO>> GetTutorsForStudentAsync(int studentId)
        {
            try
            {
                var response = await _client.GetAsync($"api/tutor-dashboard/browse-for-student/{studentId}");
                if (!response.IsSuccessStatusCode)
                    return new List<BrowseTutorDTO>();

                return await response.Content.ReadFromJsonAsync<List<BrowseTutorDTO>>() ?? new List<BrowseTutorDTO>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting tutors for student: {ex.Message}");
                return new List<BrowseTutorDTO>();
            }
        }


        public async Task<dynamic> GetReportOptionsAsync()
        {
            AddAuthHeader();
            return await _client.GetFromJsonAsync<dynamic>("api/admin/report-options");
        }


        public async Task<ReportResultDTO> GenerateReportAsync(ReportFilterDTO filters)
        {
            try
            {
                AddAuthHeader();
                var response = await _client.PostAsJsonAsync("api/admin/generate-report", filters);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new HttpRequestException($"Report generation failed: {GetUserFriendlyErrorMessage(response.StatusCode, errorContent)}");
                }

                return await response.Content.ReadFromJsonAsync<ReportResultDTO>();
            }
            catch (HttpRequestException)
            {
                throw; // Re-throw our custom messages
            }
            catch (Exception ex)
            {
                throw new HttpRequestException($"Unable to generate report. Please try again later. ({ex.Message})");
            }
        }

        public async Task<FileExportResult> GenerateReportFileAsync(ReportFilterDTO filters)
        {
            try
            {
                Console.WriteLine("=== API SERVICE DEBUG ===");
                Console.WriteLine($"ReportType: '{filters?.ReportType}'");
                Console.WriteLine($"DateRange: '{filters?.DateRange}'");
                Console.WriteLine($"ExportFormat: '{filters?.ExportFormat}'");
                Console.WriteLine($"UserType: '{filters?.UserType}'");
                Console.WriteLine($"UserId: {filters?.UserId}");
                Console.WriteLine($"StartDate: {filters?.StartDate}");
                Console.WriteLine($"EndDate: {filters?.EndDate}");
                Console.WriteLine($"Statuses: {(filters?.Statuses != null ? string.Join(", ", filters.Statuses) : "null")} (Count: {filters?.Statuses?.Count})");
                Console.WriteLine($"ModuleIds: {(filters?.ModuleIds != null ? string.Join(", ", filters.ModuleIds) : "null")} (Count: {filters?.ModuleIds?.Count})");
                Console.WriteLine("=== END API SERVICE DEBUG ===");

                AddAuthHeader();
                var response = await _client.PostAsJsonAsync("api/admin/generate-report", filters);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"API SERVICE ERROR: Status {response.StatusCode}, Content: {errorContent}");
                    throw new HttpRequestException($"Report generation failed: {GetUserFriendlyErrorMessage(response.StatusCode, errorContent)}");
                }

                var content = await response.Content.ReadAsByteArrayAsync();
                var contentType = response.Content.Headers.ContentType?.MediaType ?? "application/octet-stream";
                var fileName = GetExportFileName(filters);

                return new FileExportResult
                {
                    Content = content,
                    ContentType = contentType,
                    FileName = fileName
                };
            }
            catch (HttpRequestException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"API SERVICE UNEXPECTED ERROR: {ex.Message}");
                throw new HttpRequestException($"Unable to generate report file. Please try again later. ({ex.Message})");
            }
        }

        private string GetUserFriendlyErrorMessage(HttpStatusCode statusCode, string errorContent)
        {
            return statusCode switch
            {
                HttpStatusCode.BadRequest when errorContent.Contains("Invalid report type", StringComparison.OrdinalIgnoreCase)
                    => "The selected report type is not valid. Please choose a different report type.",
                HttpStatusCode.BadRequest when errorContent.Contains("date", StringComparison.OrdinalIgnoreCase)
                    => "The date range selected is not valid. Please check your date filters.",
                HttpStatusCode.BadRequest when errorContent.Contains("module", StringComparison.OrdinalIgnoreCase)
                    => "One or more selected modules are not valid. Please check your module selections.",
                HttpStatusCode.BadRequest
                    => "The report filters contain invalid data. Please check your selections and try again.",
                HttpStatusCode.Unauthorized
                    => "Your session has expired. Please log in again to generate reports.",
                HttpStatusCode.Forbidden
                    => "You don't have permission to generate reports. Please contact your administrator.",
                HttpStatusCode.NotFound
                    => "The report service is currently unavailable. Please try again later.",
                HttpStatusCode.InternalServerError
                    => "The report service encountered an unexpected error. Our team has been notified. Please try again later.",
                _ => "An unexpected error occurred while generating the report. Please try again."
            };
        }



        // In ApiService.cs - Add these methods
        // Add these methods to your existing ApiService
        public async Task<bool> ReportSessionCompletedAsync(int bookingId)
        {
            AddAuthHeader();
            try
            {
                var response = await _client.PutAsync($"api/bookings/complete-session/{bookingId}", null);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reporting session completion: {ex.Message}");
                return false;
            }
        }

        // In your WebApp ApiService - ensure these methods are correct
        // In your ApiService - enhance the GetGamificationProfileAsync method
        public async Task<GamificationProfileDTO> GetGamificationProfileAsync()
        {
            AddAuthHeader();
            try
            {
                var response = await _client.GetAsync("api/gamification/profile");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"=== GAMIFICATION PROFILE API RESPONSE ===");
                    Console.WriteLine($"Status: {response.StatusCode}");
                    Console.WriteLine($"Content: {content}");
                    Console.WriteLine($"Content Length: {content.Length}");

                    var profile = JsonConvert.DeserializeObject<GamificationProfileDTO>(content);

                    // Log detailed achievement info
                    if (profile != null)
                    {
                        Console.WriteLine($"=== PROFILE DETAILS ===");
                        Console.WriteLine($"User ID: {profile.UserId}");
                        Console.WriteLine($"XP: {profile.ExperiencePoints}");
                        Console.WriteLine($"Level: {profile.Level}");
                        Console.WriteLine($"Achievements Count: {profile.Achievements?.Count ?? 0}");

                        if (profile.Achievements != null)
                        {
                            foreach (var achievement in profile.Achievements)
                            {
                                Console.WriteLine($"  - {achievement.Name}: Progress={achievement.Progress}, IsCompleted={achievement.IsCompleted}, EarnedAt={achievement.EarnedAt}");
                            }
                        }
                    }

                    return profile;
                }
                else
                {
                    Console.WriteLine($"=== API ERROR ===");
                    Console.WriteLine($"Status: {response.StatusCode}");
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Error: {errorContent}");
                }
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"=== EXCEPTION ===");
                Console.WriteLine($"Error getting gamification profile: {ex.Message}");
                Console.WriteLine($"Stack: {ex.StackTrace}");
                return null;
            }
        }

        public async Task<List<Achievement>> GetAchievementsAsync()
        {
            AddAuthHeader();
            try
            {
                var response = await _client.GetAsync("api/gamification/achievements");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Achievements API Response: {content}"); // Debug log
                    return JsonConvert.DeserializeObject<List<Achievement>>(content);
                }
                Console.WriteLine($"Failed to get achievements: {response.StatusCode}");
                return new List<Achievement>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting achievements: {ex.Message}");
                return new List<Achievement>();
            }
        }


        

        // In your WebApp ApiService
        public async Task<XPBreakdownDTO> GetXPBreakdownAsync()
        {
            AddAuthHeader();
            try
            {
                var response = await _client.GetAsync("api/gamification/xp-breakdown");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    return JsonConvert.DeserializeObject<XPBreakdownDTO>(content);
                }
                Console.WriteLine($"Failed to get XP breakdown: {response.StatusCode}");
                return new XPBreakdownDTO();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting XP breakdown: {ex.Message}");
                return new XPBreakdownDTO();
            }
        }

        public async Task<List<XPActivityDTO>> GetRecentXPActivityAsync()
        {
            AddAuthHeader();
            try
            {
                var response = await _client.GetAsync("api/gamification/recent-xp-activity");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    return JsonConvert.DeserializeObject<List<XPActivityDTO>>(content);
                }
                Console.WriteLine($"Failed to get recent XP activity: {response.StatusCode}");
                return new List<XPActivityDTO>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting recent XP activity: {ex.Message}");
                return new List<XPActivityDTO>();
            }
        }


        public async Task<TutorMaterialsOverviewDTO> GetTutorMaterialsOverviewAsync(int tutorId)
        {
            AddAuthHeader();
            try
            {
                var response = await _client.GetAsync($"api/learning-materials/tutor/{tutorId}/overview");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    return JsonConvert.DeserializeObject<TutorMaterialsOverviewDTO>(content);
                }
                Console.WriteLine($"Failed to get materials overview: {response.StatusCode}");
                return new TutorMaterialsOverviewDTO();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting materials overview: {ex.Message}");
                return new TutorMaterialsOverviewDTO();
            }
        }

        public async Task<CreateFolderResponse> CreateFolderAsync(int tutorId, string name, string description, int? parentFolderId)
        {
            AddAuthHeader();
            try
            {
                Console.WriteLine($"[API SERVICE DEBUG] CreateFolderAsync START");
                Console.WriteLine($"[API SERVICE DEBUG] Parameters - TutorId: {tutorId}, Name: {name}");

                var request = new
                {
                    Name = name,
                    Description = description,
                    ParentFolderId = parentFolderId
                };

                var json = JsonConvert.SerializeObject(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                Console.WriteLine($"[API SERVICE DEBUG] Sending request to: api/learning-materials/tutor/{tutorId}/folders");
                var response = await _client.PostAsync($"api/learning-materials/tutor/{tutorId}/folders", content);

                Console.WriteLine($"[API SERVICE DEBUG] Response status: {response.StatusCode}");

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"[API SERVICE DEBUG] Success response: {responseContent}");

                    var folder = JsonConvert.DeserializeObject<LearningMaterialFolderDTO>(responseContent);
                    Console.WriteLine($"[API SERVICE DEBUG] CreateFolderAsync SUCCESS");
                    return new CreateFolderResponse { IsSuccess = true, Folder = folder };
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"[API SERVICE DEBUG] Error response: {errorContent}");
                    return new CreateFolderResponse { IsSuccess = false, ErrorMessage = errorContent };
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[API SERVICE DEBUG] Exception: {ex.Message}");
                Console.WriteLine($"[API SERVICE DEBUG] Stack trace: {ex.StackTrace}");
                return new CreateFolderResponse { IsSuccess = false, ErrorMessage = ex.Message };
            }
        }

        public async Task<UploadMaterialResponse> UploadMaterialAsync(int tutorId, string title, string description, int? folderId, bool isPublic, IFormFile file)
        {
            AddAuthHeader();
            try
            {
                Console.WriteLine($"[API SERVICE] Uploading material - TutorId: {tutorId}, Title: {title}, File: {file.FileName}");

                using var formData = new MultipartFormDataContent();

                formData.Add(new StringContent(title), "Title");
                formData.Add(new StringContent(description ?? ""), "Description");

                if (folderId.HasValue)
                    formData.Add(new StringContent(folderId.Value.ToString()), "FolderId");

                formData.Add(new StringContent(isPublic.ToString()), "IsPublic");

                if (file != null)
                {
                    var fileContent = new StreamContent(file.OpenReadStream());
                    fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(file.ContentType);
                    formData.Add(fileContent, "File", file.FileName);
                }

                var response = await _client.PostAsync($"api/learning-materials/tutor/{tutorId}/upload", formData);

                Console.WriteLine($"[API SERVICE] Upload response status: {response.StatusCode}");

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"[API SERVICE] Upload success response: {responseContent}");

                    var material = JsonConvert.DeserializeObject<LearningMaterialDTO>(responseContent);
                    return new UploadMaterialResponse { IsSuccess = true, Material = material };
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"[API SERVICE] Upload error response: {errorContent}");
                    return new UploadMaterialResponse { IsSuccess = false, ErrorMessage = errorContent };
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[API SERVICE] Upload exception: {ex.Message}");
                return new UploadMaterialResponse { IsSuccess = false, ErrorMessage = ex.Message };
            }
        }

        public async Task<LearningMaterialFolderDTO> GetFolderContentsAsync(int tutorId, int folderId)
        {
            AddAuthHeader();
            try
            {
                var response = await _client.GetAsync($"api/learning-materials/tutor/{tutorId}/folders/{folderId}");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    return JsonConvert.DeserializeObject<LearningMaterialFolderDTO>(content);
                }
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting folder contents: {ex.Message}");
                return null;
            }
        }

        public async Task<bool> DeleteMaterialAsync(int materialId)
        {
            AddAuthHeader();
            try
            {
                var response = await _client.DeleteAsync($"api/learning-materials/materials/{materialId}");
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting material: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> DeleteFolderAsync(int folderId)
        {
            AddAuthHeader();
            try
            {
                Console.WriteLine($"[API SERVICE] Deleting folder {folderId}");
                var response = await _client.DeleteAsync($"api/learning-materials/folders/{folderId}");

                Console.WriteLine($"[API SERVICE] Delete folder response: {response.StatusCode}");
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[API SERVICE] Error deleting folder: {ex.Message}");
                return false;
            }
        }

        // Add these to your ApiService

        public async Task<List<LearningMaterialDTO>> GetStudentMaterialsAsync(int studentId)
        {
            AddAuthHeader();
            try
            {
                var response = await _client.GetAsync($"api/student/{studentId}/materials");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    return JsonConvert.DeserializeObject<List<LearningMaterialDTO>>(content) ?? new List<LearningMaterialDTO>();
                }
                return new List<LearningMaterialDTO>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting student materials: {ex.Message}");
                return new List<LearningMaterialDTO>();
            }
        }

        public async Task<List<LearningMaterialDTO>> GetTutorMaterialsForStudentAsync(int studentId, int tutorId)
        {
            AddAuthHeader();
            try
            {
                var response = await _client.GetAsync($"api/student/{studentId}/materials/tutor/{tutorId}");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    return JsonConvert.DeserializeObject<List<LearningMaterialDTO>>(content) ?? new List<LearningMaterialDTO>();
                }
                return new List<LearningMaterialDTO>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting tutor materials for student: {ex.Message}");
                return new List<LearningMaterialDTO>();
            }
        }

        public async Task<StudentMaterialsOverviewDTO> GetStudentMaterialsOverviewAsync(int studentId)
        {
            AddAuthHeader();
            try
            {
                Console.WriteLine($"[API SERVICE] Getting student materials overview for student {studentId}");
                var response = await _client.GetAsync($"api/student/{studentId}/materials/overview");

                Console.WriteLine($"[API SERVICE] Overview response status: {response.StatusCode}");

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"[API SERVICE] Overview content: {content}");

                    // Deserialize to strongly-typed DTO
                    var overview = JsonConvert.DeserializeObject<StudentMaterialsOverviewDTO>(content);
                    Console.WriteLine($"[API SERVICE] Strongly-typed overview - TotalMaterials: {overview?.TotalMaterials}, TotalTutors: {overview?.TotalTutors}");

                    return overview ?? new StudentMaterialsOverviewDTO();
                }
                else
                {
                    Console.WriteLine($"[API SERVICE] Overview error: {response.StatusCode}");
                }
                return new StudentMaterialsOverviewDTO();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[API SERVICE] Exception getting overview: {ex.Message}");
                return new StudentMaterialsOverviewDTO();
            }
        }



        public class FileExportResult
{
    public byte[] Content { get; set; }
    public string ContentType { get; set; }
    public string FileName { get; set; }
}

private string GetExportFileName(ReportFilterDTO filters)
{
    var reportType = filters.ReportType?.ToLower() ?? "report";
    var extension = filters.ExportFormat?.ToLower() ?? "file";
    return $"{reportType}_report_{DateTime.UtcNow:yyyyMMdd_HHmmss}.{extension}";
}

        // Update these methods in your ApiService (WebApp)
        public async Task<bool> ChangePasswordAsync(ChangePasswordDTO dto)
        {
            AddAuthHeader();
            try
            {
                Console.WriteLine($"[WebApp] Changing password...");
                Console.WriteLine($"[WebApp] Sending - Current: {dto.CurrentPassword}, New: {dto.NewPassword}, Confirm: {dto.ConfirmPassword}");

                // The DTO properties are PascalCase, which matches the API expectation
                var data = new
                {
                    CurrentPassword = dto.CurrentPassword,
                    NewPassword = dto.NewPassword,
                    ConfirmPassword = dto.ConfirmPassword
                };

                var response = await _client.PutAsJsonAsync("api/student/change-password", data);
                var responseContent = await response.Content.ReadAsStringAsync();

                Console.WriteLine($"[WebApp] Change password response: {response.StatusCode}");
                Console.WriteLine($"[WebApp] Response content: {responseContent}");

                if (!response.IsSuccessStatusCode)
                {
                    throw new HttpRequestException(responseContent);
                }

                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[WebApp] Error changing password: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> DeleteAccountAsync()
        {
            AddAuthHeader();
            try
            {
                var student = await GetStudentByUserIdAsync();
                if (student == null) return false;

                var response = await _client.DeleteAsync($"api/student/delete-account/{student.StudentId}");
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting account: {ex.Message}");
                return false;
            }
        }




        // Add to your existing ApiService
        public async Task<ChatResponse> AskChatbotAsync(string question, int? conversationId = null, string context = null)
        {
            AddAuthHeader();
            try
            {
                Console.WriteLine($"[ApiService] Asking chatbot: {question}");
                Console.WriteLine($"[ApiService] ConversationId: {conversationId}");

                var request = new ChatQuestionRequest
                {
                    Question = question,
                    ConversationId = conversationId,
                    Context = context
                };

                Console.WriteLine($"[ApiService] Sending request to: {_client.BaseAddress}api/chatbot/ask");
                var response = await _client.PostAsJsonAsync("api/chatbot/ask", request);

                Console.WriteLine($"[ApiService] Response status: {response.StatusCode}");

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"[ApiService] Error response: {errorContent}");
                    throw new HttpRequestException($"Chatbot API error: {response.StatusCode} - {errorContent}");
                }

                var chatResponse = await response.Content.ReadFromJsonAsync<ChatResponse>();
                Console.WriteLine($"[ApiService] ✅ Got chatbot response: {chatResponse?.Answer?.Length ?? 0} chars");

                return chatResponse;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ApiService] ❌ Error asking chatbot: {ex.Message}");
                throw;
            }
        }

        public async Task<List<ConversationDTO>> GetChatbotConversationsAsync()
        {
            AddAuthHeader();
            try
            {
                Console.WriteLine($"[ApiService] Getting chatbot conversations");
                var conversations = await _client.GetFromJsonAsync<List<ConversationDTO>>("api/chatbot/conversations");
                Console.WriteLine($"[ApiService] ✅ Got {conversations?.Count ?? 0} conversations");
                return conversations ?? new List<ConversationDTO>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ApiService] ❌ Error getting conversations: {ex.Message}");
                return new List<ConversationDTO>();
            }
        }

        public async Task<bool> DeleteChatbotConversationAsync(int conversationId)
        {
            AddAuthHeader();
            try
            {
                Console.WriteLine($"[ApiService] Deleting conversation {conversationId}");
                var response = await _client.DeleteAsync($"api/chatbot/conversations/{conversationId}");
                Console.WriteLine($"[ApiService] Delete response: {response.StatusCode}");
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ApiService] ❌ Error deleting conversation: {ex.Message}");
                return false;
            }
        }



        // Add to ApiService class
        public async Task<List<CourseDTO>> GetCoursesAsync()
        {
            AddAuthHeader();
            try
            {
                var response = await _client.GetAsync("api/admin/courses");
                if (!response.IsSuccessStatusCode)
                    return new List<CourseDTO>();

                return await response.Content.ReadFromJsonAsync<List<CourseDTO>>() ?? new List<CourseDTO>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting courses: {ex.Message}");
                return new List<CourseDTO>();
            }
        }

        public async Task<bool> CreateCourseAsync(CreateCourseDTO dto)
        {
            AddAuthHeader();
            try
            {
                var response = await _client.PostAsJsonAsync("api/admin/courses", dto);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating course: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> UpdateCourseAsync(UpdateCourseDTO dto)
        {
            AddAuthHeader();
            try
            {
                var response = await _client.PutAsJsonAsync($"api/admin/courses/{dto.CourseId}", dto);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating course: {ex.Message}");
                return false;
            }
        }

        public async Task<(bool Success, string Message)> DeleteCourseAsync(int id)
        {
            AddAuthHeader();
            try
            {
                var response = await _client.DeleteAsync($"api/admin/courses/{id}");

                if (response.IsSuccessStatusCode)
                {
                    return (true, "Course deleted successfully.");
                }

                var errorContent = await response.Content.ReadAsStringAsync();
                return (false, errorContent);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting course: {ex.Message}");
                return (false, $"Error deleting course: {ex.Message}");
            }
        }

        // Optional: Additional methods for course details
        public async Task<List<ModuleDTO>> GetModulesByCourseAsync(int courseId)
        {
            AddAuthHeader();
            try
            {
                var response = await _client.GetAsync($"api/admin/courses/{courseId}/modules");
                if (!response.IsSuccessStatusCode)
                    return new List<ModuleDTO>();

                return await response.Content.ReadFromJsonAsync<List<ModuleDTO>>() ?? new List<ModuleDTO>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting course modules: {ex.Message}");
                return new List<ModuleDTO>();
            }
        }

        public async Task<List<TutorDTO>> GetTutorsByCourseAsync(int courseId)
        {
            AddAuthHeader();
            try
            {
                var response = await _client.GetAsync($"api/admin/courses/{courseId}/tutors");
                if (!response.IsSuccessStatusCode)
                    return new List<TutorDTO>();

                return await response.Content.ReadFromJsonAsync<List<TutorDTO>>() ?? new List<TutorDTO>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting course tutors: {ex.Message}");
                return new List<TutorDTO>();
            }
        }

        public async Task<List<StudentDTO>> GetStudentsByCourseAsync(int courseId)
        {
            AddAuthHeader();
            try
            {
                var response = await _client.GetAsync($"api/admin/courses/{courseId}/students");
                if (!response.IsSuccessStatusCode)
                    return new List<StudentDTO>();

                return await response.Content.ReadFromJsonAsync<List<StudentDTO>>() ?? new List<StudentDTO>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting course students: {ex.Message}");
                return new List<StudentDTO>();
            }
        }


        // Add these methods to your existing ApiService

        public async Task<bool> UpdateThemePreferenceAsync(string theme)
        {
            AddAuthHeader();
            try
            {
                Console.WriteLine($"[ApiService] Updating theme preference to: {theme}");

                var response = await _client.PostAsJsonAsync("api/auth/update-theme", new { theme });

                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"[ApiService] Theme preference updated successfully");
                    return true;
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"[ApiService] Failed to update theme: {errorContent}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ApiService] Error updating theme: {ex.Message}");
                return false;
            }
        }

        public async Task<string> GetThemePreferenceAsync()
        {
            AddAuthHeader();
            try
            {
                Console.WriteLine($"[ApiService] Getting theme preference");

                var response = await _client.GetAsync("api/auth/theme-preference");

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();
                    var theme = result?["theme"] ?? "light";
                    Console.WriteLine($"[ApiService] Retrieved theme: {theme}");
                    return theme;
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"[ApiService] Failed to get theme preference: {errorContent}, using default");
                    return "light";
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ApiService] Error getting theme preference: {ex.Message}");
                return "light";
            }
        }

        

        public class CreateModuleRequest
        {
            public string Code { get; set; }
            public string Name { get; set; }
            public int CourseId { get; set; }
        }

        public class UpdateModuleRequest
        {
            public string Code { get; set; }
            public string Name { get; set; }
            public int CourseId { get; set; }
        }

    }
}