﻿using Abp.Authorization;
using classifieds.Authorization.Users;
using classifieds.Controllers;
using classifieds.Models.ManageViewModels;
using classifieds.Services;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace classifieds.Web.Controllers.api
{
    
    [ApiController]
    [AbpAuthorize]
    [Route("api/[controller]/[action]")]
    [IgnoreAntiforgeryToken]
    public class AccountController: classifiedsControllerBase
    {
        private readonly UserManager _userManager;
        private readonly ISmsSender _smsSender;
        public AccountController(UserManager userManager, ISmsSender smsSender)
        {
            _userManager = userManager;
            _smsSender = smsSender;
        }

        [HttpPost]

        public async Task<ActionResult> AddPhoneNumber(AddPhoneNumberViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest("phone is invalid.");
            }
            // Generate the token and send it
            var user = await _userManager.GetUserByIdAsync(AbpSession.UserId.Value);
            if (user == null)
            {
                return BadRequest();
            }
            var code = await _userManager.GenerateChangePhoneNumberTokenAsync(user, model.PhoneNumber);
            try
            {
                await _smsSender.SendSmsAsync(model.PhoneNumber, "کد تایید شما :  " + code);
                return Ok();

            }
            catch (System.Exception e)
            {
                ModelState.AddModelError("sms", e.Message);
                return StatusCode(500);
            }
        }
        [HttpPost]
        public async Task<IActionResult> VerifyPhoneNumber(VerifyPhoneNumberViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            var user = await _userManager.GetUserAsync(HttpContext.User);
            if (user != null)
            {
                var result = await _userManager.ChangePhoneNumberAsync(user, model.PhoneNumber, model.Code);
                if (result.Succeeded)
                {
                    return  Ok();
                }
            }
            // If we got this far, something failed, redisplay the form
            ModelState.AddModelError(string.Empty, "Failed to verify phone number");
            return BadRequest(model);
        }
    }
}
