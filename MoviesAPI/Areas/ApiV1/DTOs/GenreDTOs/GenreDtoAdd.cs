﻿using MoviesAPI.Validations;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MoviesAPI.Areas.ApiV1.DTOs.GenreDTOs
{
    public class GenreDtoAdd
    {
        [StringLength(40)]
        [FirstLetterUpperCase]
        public string Name { get; set; }
    }
}