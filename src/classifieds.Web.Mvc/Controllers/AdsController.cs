﻿using Abp.Application.Services.Dto;
using Abp.Runtime.Validation;
using Abp.Web.Models;
using classifieds.Amenities;
using classifieds.Amenities.Dto;
using classifieds.Categories;
using classifieds.Categories.Dto;
using classifieds.Cities;
using classifieds.Cities.Dto;
using classifieds.Controllers;
using classifieds.Districts;
using classifieds.Districts.Dto;
using classifieds.Posts;
using classifieds.Posts.Dto;
using classifieds.PostsAmenities.Dto;
using classifieds.PropertyTypes;
using classifieds.PropertyTypes.Dto;
using classifieds.Web.Models.Ads;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace classifieds.Web.Controllers
{
    public class AdsController : classifiedsControllerBase
    {

        private readonly IPostAppService _postService;
        private readonly IDistrictAppService _districtService;
        private readonly ITypeAppService _typeService;
        private readonly ICategoryAppService _categoryService;
        private readonly AmenityAppService _amenityService;
        private readonly ICityAppService _cityService;

        public AdsController(
            IPostAppService postService,
            ICategoryAppService categoryService,
            IDistrictAppService districtService,
            ITypeAppService typeService,
             AmenityAppService amenityService
            , ICityAppService cityService)
        {
            _postService = postService;
            _categoryService = categoryService;
            _districtService = districtService;
            _typeService = typeService;
            _amenityService = amenityService;
            _cityService = cityService;
        }
        public async Task<IActionResult> Index(GetAllPostsInput inputs)
        {
            var types = (await _typeService.GetAllAsync(new PagedAndSortedResultRequestDto())).Items.ToList();
            var cities = (await _cityService.GetAllAsync(new PagedAndSortedResultRequestDto { MaxResultCount = int.MaxValue })).Items.ToList();
            var districts = new List<DistrictDto>();
            if (inputs.City != null)
            {
                 districts.AddRange((await _districtService.GetByCityId(inputs.City.Value)).ToList());
            }
            cities.Insert(0, new CityDto { Id = 0, Name = "شهر را انتخاب کنید" });
            types.Insert(0, new PropertyTypeDto { Id = 0, Name = "نوع ملک را انتخاب کنید" });
            districts.Insert(0, new DistrictDto { Id = 0, Name = " ناحیه را انتخاب کنید" });

            ViewData["Categories"] = new SelectList((await _categoryService.GetAllAsync(new PagedAndSortedResultRequestDto())).Items, nameof(CategoryDto.Id), nameof(CategoryDto.Name),inputs.Category);
            ViewData["Districts"] = new SelectList(districts, "Id", "Name",inputs.District);
            ViewData["Cities"] = new SelectList(cities,nameof(CityDto.Id), nameof(CityDto.Name),inputs.City);
            //ViewData["Amenitites"] = (await _amenityService.GetAllAsync(new PagedAndSortedResultRequestDto())).Items;

            if (inputs.Types == null)
            {
                ViewData["PropertyTypes"] = new SelectList(types, nameof(PropertyTypeDto.Id), nameof(PropertyTypeDto.Name));

            }
            else
            {
                ViewData["PropertyTypes"] = new SelectList(types, nameof(PropertyTypeDto.Id), nameof(PropertyTypeDto.Name), inputs.Types[0]);
            }

            return View(inputs);
        }
        [Route("[controller]/{id:int:required}")]
        public async Task<IActionResult> Show(int id)
        {
            var post = await _postService.GetDetails(id);
            if (post != null && post.IsVerified)
            {
                var recommendations = (await _postService.Recommendations(post)).Items;
                var model = new ShowAdViewModel { Post = post,Recommendations= recommendations };
               return View(model);
            }
            return NotFound();
        }
        public async Task<IActionResult> Create()
        {
            var cities = (await _cityService.GetAllAsync(new PagedAndSortedResultRequestDto { MaxResultCount = int.MaxValue })).Items.ToList();
            cities.Insert(0, new CityDto { Id = 0, Name = "شهر را انتخاب کنید" });
            ViewData["Cities"] = new SelectList(cities, nameof(CityDto.Id), nameof(CityDto.Name));
            ViewData["Amenities"] = (await _amenityService.GetAllAsync(new PagedAndSortedResultRequestDto())).Items;
            ViewData["Categories"] = new SelectList((await _categoryService.GetAllAsync(new PagedAndSortedResultRequestDto())).Items, "Id", "Name");
            ViewData["Districts"] = new SelectList((await _districtService.GetAllAsync(new PagedAndSortedResultRequestDto())).Items, "Id", "Name");
            ViewData["PropertyTypes"] = new SelectList((await _typeService.GetAllAsync(new PagedAndSortedResultRequestDto())).Items, "Id", "Name");


            return View();
        }
        [HttpPost]
        [DisableValidation]

        public async Task<IActionResult> Create([FromBody] AdsViewModel inputs)
        {
            if (ModelState.IsValid)
            {
                var inputsPost = inputs.ToPost();
    
                var post = await _postService.CreateAsync(inputsPost);
                return Json(post.Id);
            }
            else
            {

                ViewData["Categories"] = new SelectList((await _categoryService.GetAllAsync(new PagedAndSortedResultRequestDto())).Items, "Id", "Name");
                ViewData["Districts"] = new SelectList((await _districtService.GetAllAsync(new PagedAndSortedResultRequestDto())).Items, "Id", "Name");
                ViewData["PropertyTypes"] = new SelectList((await _typeService.GetAllAsync(new PagedAndSortedResultRequestDto())).Items, "Id", "Name");
                return BadRequest(ModelState);
            }
        }
    }
}
