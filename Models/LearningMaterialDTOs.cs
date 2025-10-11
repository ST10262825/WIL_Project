// Models/LearningMaterialDTOs.cs
namespace TutorConnect.WebApp.Models
{
    public class LearningMaterialDTO
    {
        public int LearningMaterialId { get; set; }
        public int TutorId { get; set; }
        public string TutorName { get; set; } 
        public int? FolderId { get; set; }
        public string FolderName { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string FileName { get; set; }
        public string FileUrl { get; set; }
        public string FileType { get; set; }
        public long FileSize { get; set; }
        public string FileSizeFormatted { get; set; }
        public bool IsPublic { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class LearningMaterialFolderDTO
    {
        public int FolderId { get; set; }
        public int TutorId { get; set; }
        public int? ParentFolderId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public DateTime CreatedAt { get; set; }
        public int MaterialCount { get; set; }
        public int SubfolderCount { get; set; }
        public List<LearningMaterialDTO> Materials { get; set; } = new List<LearningMaterialDTO>();
        public List<LearningMaterialFolderDTO> Subfolders { get; set; } = new List<LearningMaterialFolderDTO>();
    }

    public class TutorMaterialsOverviewDTO
    {
        public int TotalMaterials { get; set; }
        public int TotalFolders { get; set; }
        public long TotalStorageUsed { get; set; }
        public string TotalStorageFormatted { get; set; }
        public List<LearningMaterialFolderDTO> RootFolders { get; set; } = new List<LearningMaterialFolderDTO>();
        public List<LearningMaterialDTO> RecentMaterials { get; set; } = new List<LearningMaterialDTO>();
    }

    public class ApiResponse<T>
    {
        public bool IsSuccess { get; set; }
        public T Data { get; set; }
        public string ErrorMessage { get; set; }
    }

    public class CreateFolderResponse
    {
        public bool IsSuccess { get; set; }
        public LearningMaterialFolderDTO Folder { get; set; }
        public string ErrorMessage { get; set; }
    }

    public class UploadMaterialResponse
    {
        public bool IsSuccess { get; set; }
        public LearningMaterialDTO Material { get; set; }
        public string ErrorMessage { get; set; }
    }
}