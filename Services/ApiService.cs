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
            var response = await _client.GetAsync($"api/tutor-dashboard/user/{userId}");
            if (!response.IsSuccessStatusCode)
                throw new HttpRequestException($"API returned error or empty response: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}");

            return await response.Content.ReadFromJsonAsync<TutorDTO>();
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


        public async Task<T> GetAsync<T>(string url)
        {
            var response = await _client.GetAsync(url);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<T>();
        }

        public async Task<int> GetUnreadMessagesCountAsync(int userId)
        {
            AddAuthHeader();
            // Ensure the URL matches your API route exactly
            return await _client.GetFromJsonAsync<int>($"api/chat/unread-count/{userId}");
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



        public async Task<(bool IsSuccess, string ErrorMessage, string? ProfileImageUrl)>
     UpdateTutorProfileAsync(int tutorId, string bio, string? aboutMe, string? expertise, string? education, IFormFile? profileImage)
        {
            using var form = new MultipartFormDataContent();

            // Add all text fields
            form.Add(new StringContent(bio ?? ""), "Bio");
            form.Add(new StringContent(aboutMe ?? ""), "AboutMe");
            form.Add(new StringContent(expertise ?? ""), "Expertise");
            form.Add(new StringContent(education ?? ""), "Education");

            // Add profile image if exists
            if (profileImage != null)
            {
                var streamContent = new StreamContent(profileImage.OpenReadStream());
                form.Add(streamContent, "ProfileImage", profileImage.FileName);
            }

            // Send PUT request to API
            var response = await _client.PutAsync($"api/tutor-dashboard/{tutorId}/profile", form);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                return (false, error, null);
            }

            // Read updated profile info from API
            var json = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();
            string? updatedUrl = null;

            if (json != null && json.TryGetValue("profileImageUrl", out var url) && !string.IsNullOrEmpty(url))
            {
                // Ensure absolute URL
                if (Uri.IsWellFormedUriString(url, UriKind.Absolute))
                    updatedUrl = url;
                else
                    updatedUrl = new Uri(_client.BaseAddress!, url).ToString();
            }

            return (true, null, updatedUrl);
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

        public async Task<(bool Success, string Message)> DeleteStudentAsync(int id)
        {
            AddAuthHeader();
            try
            {
                Console.WriteLine($"Attempting to delete student with ID: {id}");

                var response = await _client.DeleteAsync($"api/admin/delete-student/{id}");

                Console.WriteLine($"Delete response status: {response.StatusCode}");

                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine("Student deleted successfully");
                    return (true, "Student deleted successfully.");
                }

                // Handle specific error cases
                var errorContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Delete error content: {errorContent}");

                if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
                {
                    if (errorContent.Contains("bookings", StringComparison.OrdinalIgnoreCase))
                    {
                        return (false, "Cannot delete student with existing bookings.");
                    }
                    return (false, errorContent);
                }

                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return (false, "Student not found.");
                }

                return (false, $"Failed to delete student: {errorContent}");
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"HTTP error deleting student: {ex.Message}");
                return (false, $"Network error: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting student: {ex.Message}");
                return (false, $"Error deleting student: {ex.Message}");
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

        public class CreateModuleRequest
        {
            public string Code { get; set; }
            public string Name { get; set; }
        }

        public class UpdateModuleRequest
        {
            public string Code { get; set; }
            public string Name { get; set; }
        }

    }
}