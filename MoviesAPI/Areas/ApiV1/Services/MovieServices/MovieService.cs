﻿using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using MoviesAPI.Areas.ApiV1.DTOs;
using MoviesAPI.Areas.ApiV1.DTOs.MovieDTOs;
using MoviesAPI.Areas.ApiV1.Models;
using MoviesAPI.Areas.ApiV1.Services.FileStorageServices;

using MoviesAPI.Data;
using MoviesAPI.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace MoviesAPI.Areas.ApiV1.Services.MovieServices
{
    public class MovieService : IMovieService
    {
        private readonly AppDBContext _context;
        private readonly IMapper _mapper;
        private readonly IFileStorageService _fileStorageService;
        private readonly IHttpContextAccessor _httpContext;
        private readonly string containerName = "movies";

        public MovieService(
            AppDBContext context
            , IMapper mapper
            , IFileStorageService fileStorageService
            , IHttpContextAccessor httpContext)
        {
            _context = context;
            _mapper = mapper;
            _fileStorageService = fileStorageService;
            _httpContext = httpContext;
        }

        public async Task<ServiceResponse<MovieDto>> AddMovie(MovieDtoAdd newItem)
        {
            Movie movie = _mapper.Map<Movie>(newItem);

            //return ResponseResult.Success(new MovieDto());

            if (newItem.Poster != null)
            {
                using (var memoryStream = new MemoryStream())
                {
                    await newItem.Poster.CopyToAsync(memoryStream);
                    var content = memoryStream.ToArray();
                    var extension = Path.GetExtension(newItem.Poster.FileName);

                    movie.Poster =
                        await _fileStorageService.SaveFile(content, extension, containerName, newItem.Poster.ContentType);
                }
            }

            AnnotateActorsOrder(movie);

            _context.Movies.Add(movie);

            await _context.SaveChangesAsync();

            MovieDto movieDTO = _mapper.Map<MovieDto>(movie);

            return ResponseResult.Success(movieDTO);
        }

        public async Task<ServiceResponse<MovieDto>> DeleteMovie(int id)
        {
            Movie movie = await _context.Movies.FindAsync(id);

            if (movie == null)
            {
                return ResponseResult.Failure<MovieDto>($"id = {id} Not found.");
            }

            _context.Movies.Remove(movie);

            await _context.SaveChangesAsync();

            MovieDto movieDTO = _mapper.Map<MovieDto>(movie);

            return ResponseResult.Success(movieDTO);
        }

        public async Task<ServiceResponse<MovieDtoIndex>> GetAllMovies()
        {
            var top = 6;
            var today = new DateTime(2020, 01, 01);

            var upcomingRelease = await _context.Movies
                .Where(x => x.ReleaseDate > today)
                .OrderBy(x => x.ReleaseDate)
                .Take(top)
                .ToListAsync();

            var inTheaters = await _context.Movies
                .Where(x => x.InTheaters)
                .Take(top)
                .ToListAsync();

            var result = new MovieDtoIndex();

            result.UpcomingReleases = _mapper.Map<List<MovieDto>>(upcomingRelease);
            result.InTheaters = _mapper.Map<List<MovieDto>>(inTheaters);

            return ResponseResult.Success(result);
        }

        public async Task<ServiceResponseWithPagination<List<MovieDto>>> GetAllMoviesPagination(PaginationDto pagination)
        {
            var queryable = _context.Movies.AsQueryable();
            var paginationResult = await _httpContext.HttpContext
                .InsertPaginationParametersInResponse(
                queryable
                , pagination.RecordsPerPage
                , pagination.Page);
            var movies = await queryable.Paginate(pagination).ToListAsync();

            List<MovieDto> movieDTOs = _mapper.Map<List<MovieDto>>(movies);

            return ResponseResultWithPagination.Success(movieDTOs, paginationResult);
        }

        public async Task<ServiceResponse<MovieDetailDto>> GetMovieById(int id)
        {
            Movie movie = await _context.Movies
                .Include(x => x.MoviesActors)
                .ThenInclude(x => x.Person)
                .Include(x => x.MoviesGenres).ThenInclude(x => x.Genre)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (movie == null)
            {
                return ResponseResult.Failure<MovieDetailDto>($"id = {id} Not found.");
            }

            MovieDetailDto movieDTO = _mapper.Map<MovieDetailDto>(movie);

            return ResponseResult.Success(movieDTO);
        }

        public async Task<ServiceResponse<MovieDto>> UpdateMovie(int id, MovieDtoUpdate newItem)
        {
            Movie movie = await _context.Movies.FindAsync(id);

            if (movie == null)
            {
                return ResponseResult.Failure<MovieDto>($"id = {id} Not found.");
            }

            movie = _mapper.Map(newItem, movie);

            if (newItem.Poster != null)
            {
                using (var memoryStream = new MemoryStream())
                {
                    await newItem.Poster.CopyToAsync(memoryStream);
                    var content = memoryStream.ToArray();
                    var extension = Path.GetExtension(newItem.Poster.FileName);

                    movie.Poster =
                        await _fileStorageService.EditFile(content, extension, containerName, movie.Poster, newItem.Poster.ContentType);
                }
            }

            var ma = _context.MoviesActors.Where(x => x.MovieId == movie.Id).ToList();

            _context.MoviesActors.RemoveRange(ma);

            var mg = _context.MoviesGenres.Where(x => x.MovieId == movie.Id).ToList();

            _context.MoviesGenres.RemoveRange(mg);

            //await _context.Database
            //    .ExecuteSqlInterpolatedAsync($"delete from MoviesActors where MovieId = {movie.Id}; delete from MoviesGenres where MovieId = {movie.Id};");

            AnnotateActorsOrder(movie);

            _context.Movies.Update(movie);

            await _context.SaveChangesAsync();

            MovieDto movieDTO = _mapper.Map<MovieDto>(movie);

            return ResponseResult.Success(movieDTO);
        }

        private static void AnnotateActorsOrder(Movie movie)
        {
            if (movie.MoviesActors != null)
            {
                for (int i = 0; i < movie.MoviesActors.Count; i++)
                {
                    movie.MoviesActors[i].Order = i;
                }
            }
        }

        public async Task<ServiceResponseWithPagination<List<MovieDto>>> Filter(MovieDtoFilter filter)
        {
            var query = _context.Movies.AsQueryable();

            if (!string.IsNullOrWhiteSpace(filter.Title))
            {
                query = query.Where(x => x.Title.Contains(filter.Title));
            }

            if (filter.UpcomingReleases)
            {
                query = query.Where(x => x.InTheaters);
            }

            if (filter.UpcomingReleases)
            {
                var today = new DateTime(2020, 01, 01);
                query = query.Where(x => x.ReleaseDate > today);
            }

            if (filter.GenreId != 0)
            {
                query = query.Where(x => x.MoviesGenres.Select(y => y.GenreId)
                .Contains(filter.GenreId));
            }

            var paginationResult = await _httpContext.HttpContext
                .InsertPaginationParametersInResponse(
                query
                , filter.RecordsPerPage
                , filter.Page);
            var movies = await query.Paginate(filter).ToListAsync();

            List<MovieDto> movieDTOs = _mapper.Map<List<MovieDto>>(movies);

            return ResponseResultWithPagination.Success(movieDTOs, paginationResult);
        }
    }
}