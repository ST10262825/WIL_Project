namespace TutorConnect.WebApp.Models
{
    public class StudentMaterialsOverviewDTO
    {
        public int TotalMaterials { get; set; }
        public int TotalTutors { get; set; }
        public List<TutorSimpleDTO> Tutors { get; set; } = new List<TutorSimpleDTO>();
        public List<LearningMaterialDTO> RecentMaterials { get; set; } = new List<LearningMaterialDTO>();
    }

    public class TutorSimpleDTO
    {
        public int TutorId { get; set; }
        public string TutorName { get; set; }
    }
}
