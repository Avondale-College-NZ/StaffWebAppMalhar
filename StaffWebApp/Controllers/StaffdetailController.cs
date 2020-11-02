using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using StaffWebApp.Models;

namespace StaffWebApp.Controllers
{
    public class StaffdetailController : Controller
    {
        //Registration Action
        [HttpGet]
        public ActionResult Registration()
        {
            return View();
        }
        //Registration POST Action
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Registration([Bind(Exclude = "IsEmailVerified,ActivationCode")] Staffdetail staffdetail)
        {
            bool Status = false;
            string message = "";
            //
            // Model Validation
            if (ModelState.IsValid)
            {


                #region // Does Email Exist
                var isExist = IsEmailExist(staffdetail.EmailId);
                if (isExist)
                {
                    ModelState.AddModelError("Email Exist", "Email Already Exist");
                    return View(staffdetail);
                }
                #endregion

                #region Generate Activation Code
                staffdetail.ActivationCode = Guid.NewGuid();
                #endregion
                staffdetail.IsEmailVerified = false;

                #region Save To Database
                using (StaffdirectorysEntities dc = new StaffdirectorysEntities())
                {
                    dc.Staffdetails.Add(staffdetail);
                    dc.SaveChanges();

                    // Send Email To User
                    SendVerificationLinkEmail(staffdetail.EmailId, staffdetail.ActivationCode.ToString());
                    message = "Registration successfully done. Account activation link " +
                                    " has been sent to your email id:" + staffdetail.EmailId;
                    Status = true;
                }
                #endregion

            }
            else
            {
                message = "Invalid Request";
            }
            ViewBag.Message = message;
            ViewBag.Status = Status;

            return View(staffdetail);
        }
        // Verify Account

        [HttpGet]
        public ActionResult VerifyAccount(string id)
        {
            bool Status = false;
                using (StaffdirectorysEntities dc = new StaffdirectorysEntities())
            {
                dc.Configuration.ValidateOnSaveEnabled = false; //this is to avoid Confirm Password do not match
                                                                //issue on save changes
                var v = dc.Staffdetails.Where(a => a.ActivationCode == new Guid(id)).FirstOrDefault();
                if (v != null)
                {
                    v.IsEmailVerified = true;
                    dc.SaveChanges();
                    Status = true;
                }
                else
                {
                    ViewBag.Message = "Invalid Request";
                }
            }
            ViewBag.Status = Status;
                return View();
        }

        //Login
        [HttpGet]
        public ActionResult Login()
        {
            return View();
        }

        //Login POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Login(UserLogin login, string ReturnUrl="")
        {
            string message = "";
            using (StaffdirectorysEntities dc = new StaffdirectorysEntities())
            {
                var v = dc.Staffdetails.Where(a => a.EmailId == login.EmailId).FirstOrDefault();
                if (v != null)
                {
                    if (string.Compare(login.Password,v.Password) == 0)
                    {
                        int timeout = login.RememberMe ? 525600 : 60; //525600 min = 1 year
                        var ticket = new FormsAuthenticationTicket(login.EmailId, login.RememberMe, timeout);
                        string encrypted = FormsAuthentication.Encrypt(ticket);
                        var cookie = new HttpCookie(FormsAuthentication.FormsCookieName, encrypted);
                        cookie.Expires = DateTime.Now.AddMinutes(timeout);
                        cookie.HttpOnly = true;
                        Response.Cookies.Add(cookie);


                        if (Url.IsLocalUrl(ReturnUrl))
                        {
                            return Redirect(ReturnUrl);
                        }
                        else
                        {
                            return RedirectToAction("Index", "Home");
                        }
                    }
                    else
                    {
                        message = "Invalid Credential Provided";
                    }
                }
                else
                {
                    message = "Invalid Credential Provided";
                }
            }
            ViewBag.Message = message;
            return View();
        }

        //Logout
        [Authorize]
        [HttpPost]
        public ActionResult Logout()
        {
            FormsAuthentication.SignOut();
            return RedirectToAction("Login", "Staffdetail");
        }


        [NonAction]
        public bool IsEmailExist(string EmailId)
        {
            using (StaffdirectorysEntities dc = new StaffdirectorysEntities())
            {
                var v = dc.Staffdetails.Where(a => a.EmailId == EmailId).FirstOrDefault();
                return v != null;
            }

        }

        [NonAction]
        public void SendVerificationLinkEmail(string EmailId, string activationCode)
        {
            var verifyUrl = "/Staffdetail/VerifyAccount/" + activationCode;
            var link = Request.Url.AbsoluteUri.Replace(Request.Url.PathAndQuery, verifyUrl);

            var fromEmail = new MailAddress("ac98844@avcol.school.nz", "Aziz Patel");
            var toEmail = new MailAddress(EmailId);
            var fromEmailPassword = "Mohammeddo456"; // Replace with actual password
            string subject = "Your account is successfully created!";

            string body = "<br/><br/>We are excited to tell you that your Avondale College Staff account is" +
        " successfully created. Please click on the below link to verify your account" +
        " <br/><br/><a href='" + link + "'>" + link + "</a> ";

            var smtp = new SmtpClient
            {
                Host = "smtp.office365.com",
                Port = 587,
                EnableSsl = true,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(fromEmail.Address, fromEmailPassword)
            };

            using (var message = new MailMessage(fromEmail, toEmail)
            {
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            })
                smtp.Send(message);
        }

    }

}