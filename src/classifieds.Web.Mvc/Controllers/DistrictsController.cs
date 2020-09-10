﻿using Abp.Application.Services.Dto;
using Abp.AspNetCore.Mvc.Authorization;
using Abp.AspNetCore.Mvc.Controllers;
using Abp.Web.Models;
using classifieds.Authorization;
using classifieds.Cities;
using classifieds.Districts;
using classifieds.Districts.Dto;
using classifieds.Web.Models.Districts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Threading.Tasks;

namespace classifieds.Web.Controllers
{
    [AbpMvcAuthorize(PermissionNames.Pages_District)]
    public class DistrictsController : AbpController
    {
        private readonly IDistrictAppService _districtService;
        private readonly ICityAppService _cityService;
        public DistrictsController(IDistrictAppService districtService, ICityAppService cityService)
        {
            _districtService = districtService;
            _cityService = cityService;
        }
        public async Task<ActionResult> Index()
        {
            var cities = await _cityService.GetAllAsync(new PagedAndSortedResultRequestDto() { MaxResultCount = Int32.MaxValue });
            var model = new DistrictViewModel
            {
                Cities = new SelectList(cities.Items, "Id", "Name")
            };
            return View(model);
        }

        public async Task<ActionResult> Edit(int id)
        {
            var district = await _districtService.GetAsync(new EntityDto(id));
            var cities = await _cityService.GetAllAsync(new PagedAndSortedResultRequestDto() { MaxResultCount = Int32.MaxValue });
            var model = new DistrictViewModel
            {
                District = district,
                Cities = new SelectList(cities.Items, nameof(DistrictDto.Id), nameof(DistrictDto.Name), district.CityId)
            };
            return PartialView("_EditModal", model);
        }
        public async Task<AjaxResponse> GetByCityId(int id)
        {
            var districts = await _districtService.GetByCityId(id);
            return new AjaxResponse(districts);
        }
    }
}
