﻿ using Abp.Application.Services;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Collections.Extensions;
using Abp.Domain.Repositories;
using Abp.Linq.Extensions;
using classifieds.Amenities.Dto;
using classifieds.Authorization;
using classifieds.Authorization.Users;
using classifieds.Categories;
using classifieds.Cities;
using classifieds.Districts;
using classifieds.Posts.Dto;
using classifieds.PostsAmenities;
using classifieds.PostsAmenities.Dto;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;

namespace classifieds.Posts
{
    public class PostAppService : AsyncCrudAppService<Post, PostDto, int, GetAllPostsInput, CreatePostInput, UpdatePostInput>, IPostAppService
    {
        private readonly IRepository<Post> _postRepository;
        private readonly IRepository<Category> _categoryRepository;
        private readonly IRepository<District> _districtRepository;
        private readonly IRepository<City> _cityRepository;

        private readonly IRepository<User, long> _userRepository;
        public PostAppService(IRepository<City> cityRepository,IRepository<Post> repository, IRepository<Category> categoryRepository, IRepository<User, long> userRepository, IRepository<District> districtRepository) : base(repository)
        {
            _cityRepository = cityRepository;
            _postRepository = repository;
            _categoryRepository = categoryRepository;
            CreatePermissionName = PermissionNames.Posts_Create;
            UpdatePermissionName = PermissionNames.Pages_Posts;
            DeletePermissionName = PermissionNames.Pages_Posts;
            _districtRepository = districtRepository;
            _userRepository = userRepository;
        }
        public override Task DeleteAsync(EntityDto<int> input)
        {
            throw new NotImplementedException();
        }

        protected override IQueryable<Post> CreateFilteredQuery(GetAllPostsInput input)
        {
            return base.CreateFilteredQuery(input)
                .Include(m=> m.District.City)
                .Include(m => m.Images)
                .Include(m => m.Type)
                .Include(m => m.Category)
                .Include(m=>m.PostAmenities).ThenInclude(m=>m.Amenity)
                .Where(m=>m.IsVerified ==true)
                .WhereIf(input.Featured.HasValue, t => t.IsFeatured == input.Featured.Value)
                .WhereIf(input.Category.HasValue, t => t.CategoryId == input.Category.Value)
                .WhereIf(input.District.HasValue, t => t.DistrictId == input.District.Value)
                .WhereIf(input.City.HasValue, t => t.District.City.Id == input.City.Value)
                .WhereIf(input.Age.HasValue, t => t.Age >= input.Age.Value)
                .WhereIf(input.Beds.HasValue, t => t.Bedroom >= input.Beds.Value)
                .WhereIf(input.MinArea.HasValue && input.MaxArea.HasValue, t => t.Area >= input.MinArea.Value && t.Area <= input.MaxArea.Value)
                .WhereIf(input.MinPrice.HasValue && input.MaxPrice.HasValue, t => t.Price >= input.MinPrice.Value && t.Price <= input.MaxPrice.Value)
                .WhereIf(input.MinRent.HasValue && input.MaxRent.HasValue, t => t.Rent >= input.MinRent.Value && t.Rent <= input.MaxRent.Value)
                .WhereIf(input.MinDeposit.HasValue && input.MaxDeposit.HasValue, t => t.Deposit >= input.MinDeposit.Value && t.Deposit <= input.MaxDeposit.Value)
                .WhereIf(input.Amenities != null && input.Amenities.Count > 0, t => t.PostAmenities.Any(m=>input.Amenities.Contains(m.AmenityId)))
                .WhereIf(input.Types != null && input.Types.Count > 0, t => input.Types.Contains(t.TypeId))
                .WhereIf(input.UserId != null, t => t.CreatorUserId == input.UserId)
                .OrderByDescending(m => m.CreationTime);

        }
        public async Task<PostDto> GetDetails(int id)
        {
            var item = await _postRepository.GetAllIncluding(m => m.District.City, m => m.Category).Where(m => m.Id == id)
                .Select(m => new PostDto
                {
                    Id = m.Id,
                    Bedroom = m.Bedroom,
                    Area = m.Area,
                    IsVerified = m.IsVerified,
                    DistrictId=m.DistrictId,
                    Price = m.Price,
                    Deposit = m.Deposit,
                    Rent = m.Rent,
                    CategoryId =m.CategoryId,
                    TypeId=m.TypeId,
                    IsFeatured = m.IsFeatured,
                    Type = ObjectMapper.Map<TypeViewModel>(m.Type),
                    Description = m.Description,
                    Category = ObjectMapper.Map<CategoryViewModel>(m.Category),
                    Latitude = m.Latitude,
                    Longitude = m.Longitude,
                    District = ObjectMapper.Map<DistrictViewModel>(m.District),
                    Title = m.Title,
                    CreationTime = m.CreationTime,
                    Amenities = m.PostAmenities.Select(m=>new AmenityDto {
                        Name=m.Amenity.Name,
                        Icon= m.Amenity.Icon,
                        Id=m.Amenity.Id,
                    }).ToList(),
                    Images = m.Images.Select(m => new ImageViewModel
                    {
                        Id = m.Id,
                        Path = m.Path,
                        Name = m.Name
                    }).ToList(),
                }).FirstOrDefaultAsync();
            return item;
        }
        [AbpAuthorize]
        public async Task<PagedResultDto<PostDto>> GetUserPosts(GetAllPostsInput input)
        {
            var posts = base.CreateFilteredQuery(input)
                 .Include(m => m.District.City)
                .Include(m => m.Images)
                .Include(m => m.Type)
                .Include(m => m.Category)
                .Include(m => m.PostAmenities).ThenInclude(m => m.Amenity).Where(m=>m.CreatorUserId == AbpSession.UserId).OrderByDescending(m=>m.CreationTime);
            ApplyPaging(posts, input);
             return new PagedResultDto<PostDto>(posts.Count(), ObjectMapper.Map<List<PostDto>>(await posts.ToListAsync()));
        }

        public async Task<PagedResultDto<PostDto>> Recommendations(PostDto post)
        {
            var items = await _postRepository.GetAllIncluding(m => m.District.City, m => m.Category).Where(m =>m.IsVerified && m.Id != post.Id && m.CategoryId == post.CategoryId && m.DistrictId == post.DistrictId && m.TypeId == post.TypeId)
                .Select(m => new PostDto
                {
                    Id = m.Id,
                    Bedroom = m.Bedroom,
                    Area = m.Area,
                    Type = ObjectMapper.Map<TypeViewModel>(m.Type),
                    Description = m.Description,
                    DistrictId = m.DistrictId,
                    Price = m.Price,
                    Deposit = m.Deposit,
                    Rent = m.Rent,
                    CategoryId = m.CategoryId,
                    TypeId = m.TypeId,
                    Category = ObjectMapper.Map<CategoryViewModel>(m.Category),
                    Latitude = m.Latitude,
                    Longitude = m.Longitude,
                    District = ObjectMapper.Map<DistrictViewModel>(m.District),
                    Title = m.Title,
                    IsFeatured = m.IsFeatured,
                    IsVerified = m.IsVerified,
                    CreationTime = m.CreationTime,
                    Images = m.Images.Select(m => new ImageViewModel
                    {
                        Id = m.Id,
                        Path = m.Path,
                        Name = m.Name
                    }).ToList(),
                }).Take(4).ToListAsync();
            return new PagedResultDto<PostDto>(items.Count, ObjectMapper.Map<List<PostDto>>(items));
        }
        [RemoteService(false)]
        public async Task<List<LlocationPostsCount>> CitiesPostsCount()
        {
            var categories = await _categoryRepository.GetAllListAsync();
            var postsCountPerCategory = new List<LlocationPostsCount>();
            foreach (var category in categories)
            {
                var cityId = _cityRepository.GetAll().Where(m => m.Name.Contains("کابل")).FirstOrDefault()?.Id;
                if (cityId != null)
                {
                    var counts = await _postRepository.GetAll().Where(m => m.CategoryId == category.Id && m.IsVerified == true && m.District.City.Name.Contains("کابل")).Include(m => m.District).GroupBy(m => m.DistrictId)
                     .Select(n => new PostsCountDto
                     {
                         DistrictId = n.Key,
                         Count = n.Count(),
                         DistrictName = _districtRepository.GetAll().Where(m => m.Id == n.Key).FirstOrDefault().Name,
                         CityId = cityId.Value,
                     }
                  ).OrderByDescending(m => m.Count).Take(10).ToListAsync();
                    postsCountPerCategory.Add(new LlocationPostsCount { CategoryName = category.Name, CategoryId = category.Id, Count = counts, CityName = "کابل" });
                }

            }
            return postsCountPerCategory;
        }
        [RemoteService(false)]
        public async Task<List<UserPostsCountDto>> UsersPostsCount()
        {
            var usersPostsCount =await _userRepository.GetAll().Where(m=> m.Posts.Count > 0).Select(m=>new UserPostsCountDto
            {
                Avatar = m.Avatar,
                Name = m.FullName ,
                UserName = m.UserName,
                Id= m.Id,
                PostsCount = m.Posts.Where(m=>m.IsVerified == true)
                .Count() }).OrderByDescending(m=>m.PostsCount).Take(10).ToListAsync();

            return usersPostsCount;
       }
        public override async Task<PostDto> CreateAsync(CreatePostInput input)
        {
            List<PostAmenity> amenities=new List<PostAmenity>();
            foreach (var amenity in input.Amenities)
            {
                amenities.Add(new PostAmenity(){AmenityId= amenity });
            }
            var post = ObjectMapper.Map<Post>(input);
            post.PostAmenities = amenities;   
            await _postRepository.InsertAndGetIdAsync(post);
            return ObjectMapper.Map<PostDto>(post);
        }
    }
}
