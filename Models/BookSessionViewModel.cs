using System;
using System.Collections.Generic;
using TutorConnect.WebApp.Models;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace TutorConnect.WebApp.Models
{
    public class BookSessionViewModel
    {
        [Required]
        public int StudentId { get; set; }  // Set this in controller from logged-in user info

        [Required(ErrorMessage = "Please select a tutor")]
        public int TutorId { get; set; }

        [Required(ErrorMessage = "Please select a module")]
        public int ModuleId { get; set; }

        [Required(ErrorMessage = "Please select a start time")]
        [DataType(DataType.DateTime)]
        public DateTime StartTime { get; set; }

        [Required(ErrorMessage = "Please select an end time")]
        [DataType(DataType.DateTime)]
        [DateGreaterThan("StartTime", ErrorMessage = "End time must be after start time")]
        public DateTime EndTime { get; set; }

        // Optional: For populating dropdown lists on the form
        [ValidateNever]
        public List<TutorDTO> AvailableTutors { get; set; }
        [ValidateNever]
        public List<ModuleDTO> AvailableModules { get; set; }
    }

    // Custom validation attribute to ensure EndTime > StartTime
    public class DateGreaterThanAttribute : ValidationAttribute
    {
        private readonly string _comparisonProperty;

        public DateGreaterThanAttribute(string comparisonProperty)
        {
            _comparisonProperty = comparisonProperty;
        }

        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            var currentValue = (DateTime?)value;
            var property = validationContext.ObjectType.GetProperty(_comparisonProperty);
            var comparisonValue = (DateTime?)property.GetValue(validationContext.ObjectInstance);

            if (currentValue != null && comparisonValue != null)
            {
                if (currentValue <= comparisonValue)
                {
                    return new ValidationResult(ErrorMessage);
                }
            }
            return ValidationResult.Success;
        }
    }
}
