using System.ComponentModel.DataAnnotations;


namespace TutorConnectAPI.Models
{
    public class LearningMaterialFolder
    {
        [Key]
        public int FolderId { get; set; }
        public int TutorId { get; set; }
        public int? ParentFolderId { get; set; } // For nested folders
        public string Name { get; set; }
        public string Description { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public Tutor Tutor { get; set; }
        public LearningMaterialFolder ParentFolder { get; set; }
        public ICollection<LearningMaterialFolder> Subfolders { get; set; } = new List<LearningMaterialFolder>();
        public ICollection<LearningMaterial> Materials { get; set; } = new List<LearningMaterial>();
    }
}
