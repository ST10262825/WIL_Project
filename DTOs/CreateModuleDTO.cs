namespace TutorConnectAPI.DTOs
{
    public class CreateModuleDTO
    {
        public string Code { get; set; }
        public string Name { get; set; }
        public int CourseId { get; set; }
    }

    public class UpdateModuleDTO
    {
        public string Code { get; set; }
        public string Name { get; set; }
        public int CourseId { get; set; } // ADD THIS
    }
}
