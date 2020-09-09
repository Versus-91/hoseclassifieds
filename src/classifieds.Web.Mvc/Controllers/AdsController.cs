﻿using Abp.Application.Services.Dto;
using Abp.Domain.Repositories;
using Abp.Runtime.Validation;
using classifieds.Categories;
using classifieds.Cities;
using classifieds.Controllers;
using classifieds.Districts;
using classifieds.Images;
using classifieds.Posts;
using classifieds.Posts.Dto;
using classifieds.PropertyTypes;
using classifieds.Web.Models.Ads;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Hosting;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UploadStream;

namespace classifieds.Web.Controllers
{
    public class AdsController : classifiedsControllerBase
    {

        private readonly IPostAppService _postService;
        private readonly IDistrictAppService _districtService;
        private readonly ITypeAppService _typeService;
        private readonly ICategoryAppService _categoryService;
        private readonly IHostEnvironment _environment;
        private readonly IRepository<Image> _imageService;
        public AdsController(
            IPostAppService postService,
            ICategoryAppService categoryService,
            IDistrictAppService districtService,
            IHostEnvironment environment,
            IRepository<Image> imageService,
            ICityAppService cityService,
            ITypeAppService typeService)
        {
            _postService = postService;
            _categoryService = categoryService;
            _districtService = districtService;
            _environment = environment;
            _imageService = imageService;
            _typeService = typeService;
        }
        public IActionResult Index(GetAllPostsInput inputs)
        {
            return View(inputs);
        }
        [Route("[controller]/{id:int:required}")]
        public async Task<IActionResult> Show(int id)
        {
            var post = await _postService.GetDetails(id);
            if (post != null)
            {
                return View(post);
            }
            return NotFound();
        }
        public async Task<IActionResult> Create()
        {

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
                var post = await _postService.CreateAsync(inputs.ToPost());
                //foreach (var file in inputs.Files)
                //{
                //    var randomName = Path.Combine(path, $"{Guid.NewGuid().ToString("N")}{Path.GetExtension(file.FileName).ToLower()}");
                //    using (var stream = new FileStream(randomName, FileMode.Create))
                //    {
                //        await file.CopyToAsync(stream);
                //    }
                //    await _imageService.InsertAsync(new Image
                //    {
                //        Name = file.Name,
                //        Path = randomName,
                //        Size = file.Length,
                //        PostId = post.Id
                //    });
                //}
                return Json(post.Id);
            }
            else
            {

                ViewData["Categories"] = new SelectList((await _categoryService.GetAllAsync(new PagedAndSortedResultRequestDto())).Items, "Id", "Name");
                ViewData["Districts"] = new SelectList((await _districtService.GetAllAsync(new PagedAndSortedResultRequestDto())).Items, "Id", "Name");
                ViewData["PropertyTypes"] = new SelectList((await _typeService.GetAllAsync(new PagedAndSortedResultRequestDto())).Items, "Id", "Name");
                return View();

            }
        }
        [HttpPost("[controller]/upload")]
        [DisableValidation]
        public async Task<IActionResult> Upload()
        {
            //byte[] buffer = new byte[BUF_SIZE];
            //var path = Path.Combine(_environment.ContentRootPath, "Images");

            //foreach (var s in model.Files)
            //{
            //    var randomName = Path.Combine(path, $"{Guid.NewGuid().ToString("N")}{Path.GetExtension(s.FileName).ToLower()}");
            //    using (var stream = s.OpenReadStream())
            //    {
            //        while (await stream.ReadAsync(buffer, 0, buffer.Length) > 0) ;
            //        using (var targetStream = System.IO.File.Create(randomName))
            //        {
            //            await targetStream.WriteAsync(buffer);
            //        }
            //        await _imageService.InsertAsync(new Image
            //        {
            //            Name = s.Name,
            //            Path = randomName,
            //            Size = s.Length,
            //            PostId = 19
            //        });
            //    }
            //}

            //return Ok(new
            //{
            //    model.Name,
            //    model.Description,
            //    files = model.Files.Select(x => new
            //    {
            //        x.Name,
            //        x.FileName,
            //        x.ContentDisposition,
            //        x.ContentType,
            //        x.Length
            //    })
            //});
            byte[] buffer = new byte[4096];
            List<IFormFile> files = new List<IFormFile>();

            var model = await this.StreamFiles<StreamModel>(async x =>
            {
                using (var stream = x.OpenReadStream())
                    while (await stream.ReadAsync(buffer, 0, buffer.Length) > 0) ;
                files.Add(x);
            });

            if (!ModelState.IsValid)
                return BadRequest();

            return Ok(new
            {
                Model = model,
                Files = files.Select(x => new
                {
                    x.Name,
                    x.FileName,
                    x.ContentDisposition,
                    x.ContentType,
                    x.Length
                })
            });
        }
    }
    public class UploadModel
    {
        public string Name { get; set; }
        public int Id { get; set; }
        public string Description { get; set; }
        public List<IFormFile> Files { get; set; }
    }

    class StreamModel
    {
        public string Name { get; set; }
        public string Description { get; set; }
    }
}
