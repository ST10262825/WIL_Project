using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using System;
using static System.Net.WebRequestMethods;
using System.Text.Json.Serialization;
using Newtonsoft.Json;
using TutorConnect.WebApp.Models;
using System.Text.Json;
using System.Text;
using System.Security.Claims;


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




        public async Task<StudentDashboardSummaryDTO> GetStudentDashboardSummaryAsync()
        {
            AddAuthHeader();
            var response = await _client.GetAsync("api/student/dashboard-summary");
            return await HandleResponse<StudentDashboardSummaryDTO>(response);
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


        // ✅ Get tutor bookings
        public async Task<List<BookingDTO>> GetTutorBookingsAsync(int tutorId)
        {
            var response = await _client.GetAsync($"api/bookings/tutor/{tutorId}");
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<List<BookingDTO>>();
            }
            return new List<BookingDTO>();
        }

        // ✅ Update booking status
        public async Task<bool> UpdateBookingStatusAsync(int bookingId, string status)
        {
            var response = await _client.PutAsync($"api/bookings/update-status/{bookingId}?status={status}", null);
            return response.IsSuccessStatusCode;
        }


        public async Task<bool> DeleteTutorAsync(int id)
        {
            AddAuthHeader();
            var response = await _client.DeleteAsync($"api/admin/delete-tutor/{id}");
            return response.IsSuccessStatusCode;
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
            var response = await _client.PostAsJsonAsync("api/auth/login", loginDto);
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException($"Login failed: {error}");
            }

            var json = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();
            return json["token"];
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



        public async Task<StudentDTO?> GetStudentByUserIdAsync()
        {
            AddAuthHeader();

            // Get UserId from the logged-in user claims
            var userIdClaim = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                throw new HttpRequestException("UserId claim missing. Please re-login.");

            // Call the correct API endpoint
            var response = await _client.GetAsync($"api/student/by-user/{userId}");

            if (!response.IsSuccessStatusCode)
                return null;

            var content = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<StudentDTO>(content);
        }



        public async Task<bool> CreateBookingAsync(CreateBookingDTO dto)
        {
            var json = System.Text.Json.JsonSerializer.Serialize(dto);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _client.PostAsync("api/bookings/create", content);
            return response.IsSuccessStatusCode;
        }

        public async Task<List<BookingDTO>> GetStudentBookingsAsync(int studentId)
        {
            var response = await _client.GetAsync($"api/bookings/student/{studentId}");
            if (!response.IsSuccessStatusCode)
                return new List<BookingDTO>();

            var json = await response.Content.ReadAsStringAsync();
            return System.Text.Json.JsonSerializer.Deserialize<List<BookingDTO>>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? new List<BookingDTO>();
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
            AddAuthHeader();
            var response = await _client.GetAsync("api/chat/users"); // You’ll create this endpoint in API
            return await HandleResponse<List<ChatUserDTO>>(response);
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

    }
}