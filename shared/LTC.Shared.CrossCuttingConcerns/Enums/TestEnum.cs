using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LTC.Shared.CrossCuttingConcerns.Enums
{
    public enum TestEnum
    {
        [Display(Name = "First Value")]
        FirstValue = 1,
        [Display(Name = "Second Value")]
        SecondValue = 2,
    }

    public static class TestName
    {
        public static string Text(TestEnum enumValue)
        {
            return enumValue switch
            {
                TestEnum.FirstValue => "First Value",
                TestEnum.SecondValue => "Second Value",
                _ => string.Empty,
            };
        }
    }
}
