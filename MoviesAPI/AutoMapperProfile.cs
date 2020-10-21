﻿using AutoMapper;
using MoviesAPI.DTOs;
using MoviesAPI.DTOs.MovieDTOs;
using MoviesAPI.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MoviesAPI
{
    public class AutoMapperProfile : Profile
    {
        public AutoMapperProfile()
        {
            CreateMap<Genre, GenreDto>().ReverseMap();
            CreateMap<GenreDtoAdd, Genre>();
            CreateMap<GenreDtoUpdate, Genre>();

            CreateMap<Person, PersonDto>().ReverseMap();
            CreateMap<PersonDtoAdd, Person>()
                .ForMember(x => x.Picture, options => options.Ignore());
            CreateMap<PersonDtoUpdate, Person>()
                .ForMember(x => x.Picture, options => options.Ignore());

            CreateMap<Movie, MovieDto>().ReverseMap();
            CreateMap<MovieDtoAdd, Movie>()
                .ForMember(x => x.Poster, options => options.Ignore());
            CreateMap<MovieDtoUpdate, Movie>()
                .ForMember(x => x.Poster, options => options.Ignore());
        }
    }
}