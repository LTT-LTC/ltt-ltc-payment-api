using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace LTC.Shared.CrossCuttingConcerns.Enums
{
    public enum FileTypeEnum : byte
    {
        [Display(Name = "Image")]
        Image = 1,

        [Display(Name = "Video")]
        Video = 2,

        [Display(Name = "Document")]
        Document = 3,
    }

    public static class FileTypeName
    {
        public static string Text(FileTypeEnum fileType)
        {
            return fileType switch
            {
                FileTypeEnum.Image => "Image",
                FileTypeEnum.Video => "Video",
                FileTypeEnum.Document => "Document",
                _ => "Unspecified"
            };
        }
    }
}
