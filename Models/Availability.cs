﻿namespace TutorConnectAPI.Models
{
    public class Availability
    {
        public int Id { get; set; }
        public int TutorId { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }

        public Tutor Tutor { get; set; }
    }

}
