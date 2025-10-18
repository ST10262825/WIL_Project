namespace TutorConnectAPI.DTOs
{
    public class CreateModuleRequest
    {
        public string Code { get; set; }
        public string Name { get; set; }
        public int CourseId { get; set; }
    }
}
