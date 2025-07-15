using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using TutorConnect.WebApp.Models;
using Microsoft.AspNetCore.Http;
using System;
using static System.Net.WebRequestMethods;
using System.Text.Json.Serialization;
using Newtonsoft.Json;

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
            var token = _httpContextAccessor.HttpContext?.Session.GetString("AuthToken");

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

        public async Task<TutorDTO> GetTutorByIdAsync(int id)
        {
            AddAuthHeader();
            var response = await _client.GetAsync($"api/admin/tutors/{id}");
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

        public async Task<bool> DeleteTutorAsync(int id)
        {
            AddAuthHeader();
            var response = await _client.DeleteAsync($"api/admin/delete-tutor/{id}");
            return response.IsSuccessStatusCode;
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

        public async Task<bool> BookSessionAsync(SessionBookingDTO bookingDto)
        {
            var response = await _client.PostAsJsonAsync("api/student-dashboard/book-session", bookingDto);
            return response.IsSuccessStatusCode;
        }

        public async Task<StudentDTO> GetStudentByUserIdAsync(int userId)
        {
            var response = await _client.GetAsync($"api/student-dashboard/by-user/{userId}");
            if (!response.IsSuccessStatusCode) return null;

            var content = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<StudentDTO>(content);
        }



        public async Task<List<SessionViewModel>> GetStudentSessionsAsync(int studentId)
        {
            var response = await _client.GetAsync($"api/sessions/student/{studentId}");
            if (!response.IsSuccessStatusCode) return new List<SessionViewModel>();

            var json = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<List<SessionViewModel>>(json);
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

        


        public async Task<List<SessionDTO>> GetTutorSessionsAsync(int tutorId)
        {
            var response = await _client.GetAsync($"api/tutor-dashboard/{tutorId}/sessions");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<List<SessionDTO>>();
        }

        public async Task<SessionSummaryDTO> GetTutorSessionSummaryAsync(int tutorId)
        {
            var response = await _client.GetAsync($"api/tutor-dashboard/{tutorId}/session-summary");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<SessionSummaryDTO>();
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




    }
}
