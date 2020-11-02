using StaffWebApp.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace StaffWebApp.Controllers
{
    public class HomeController : Controller
    {
        // GET: Home
        StaffdirectorysEntities db = new StaffdirectorysEntities();
        [Authorize]

        public ActionResult Index()
        {
            var users = db.Staffdetails.ToList();
            return View(users);
        }
        [HttpPost]
        public ActionResult Index(string searchTxt)
        {
            var users = db.Staffdetails.ToList();

            if (searchTxt != null)
            {
                users = db.Staffdetails.Where(x => x.FirstName.Contains(searchTxt)).ToList();
            }



            return View(users);
        }
    }
}