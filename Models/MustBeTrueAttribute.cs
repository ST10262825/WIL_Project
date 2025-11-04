using System.ComponentModel.DataAnnotations;

namespace TutorConnect.WebApp.Models
{
    public class MustBeTrueAttribute : ValidationAttribute
    {
        public override bool IsValid(object value)
        {
            return value is bool && (bool)value;
        }
    }
}