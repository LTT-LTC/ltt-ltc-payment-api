using System.ComponentModel.DataAnnotations;

namespace LTC.Shared.CrossCuttingConcerns.Enums
{
    public enum EmployeeStatusEnum
    {
        [Display(Name = "Active")]
        Active = 1,
        [Display(Name = "Deactive")]
        Deactive = 2,
    }

    public static class EmployeeStatusName
    {
        public static string Text(EmployeeStatusEnum status)
        {
            return status switch
            {
                EmployeeStatusEnum.Active => "Active",
                EmployeeStatusEnum.Deactive => "Deactive",
                _ => string.Empty,
            };
        }
    }
}
