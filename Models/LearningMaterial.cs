using System.ComponentModel.DataAnnotations;


namespace TutorConnectAPI.Models
{
    public class LearningMaterial
    {
        [Key]
        public int LearningMaterialId { get; set; }
        public int TutorId { get; set; }
        public int? FolderId { get; set; } // Null if it's in root
        public string Title { get; set; }
        public string Description { get; set; }
        public string FileName { get; set; }
        public string FilePath { get; set; }
        public string FileType { get; set; } // pdf, doc, ppt, etc.
        public long FileSize { get; set; }
        public bool IsPublic { get; set; } = false; // If students can access without booking
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public Tutor Tutor { get; set; }
        public LearningMaterialFolder Folder { get; set; }
        public ICollection<StudentMaterialAccess> StudentAccesses { get; set; } = new List<StudentMaterialAccess>();
    }
}