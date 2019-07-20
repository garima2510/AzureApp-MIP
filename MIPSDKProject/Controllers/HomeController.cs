using Microsoft.InformationProtection;
using Microsoft.InformationProtection.File;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace MIPSDKProject.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private static readonly string clientId = ConfigurationManager.AppSettings["ida:ClientId"];
        private static readonly string appName = ConfigurationManager.AppSettings["app:Name"];
        private static readonly string appVersion = ConfigurationManager.AppSettings["app:Version"];
        private static readonly string mipData = ConfigurationManager.AppSettings["MipData"];
        private readonly string mipPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, mipData);

        public ActionResult Index()
        {
            return View();
        }

        public ActionResult InvokeMIP()
        {
            string name = User.Identity.Name;
            ClaimsPrincipal principal = ClaimsPrincipal.Current;
            var tenantId = principal.FindFirst(c => c.Type == "http://schemas.microsoft.com/identity/claims/tenantid").Value;

            // Set path to bins folder.
            var path = Path.Combine(
                  Directory.GetParent(Path.GetDirectoryName(new Uri(System.Reflection.Assembly.GetExecutingAssembly().CodeBase).LocalPath)).FullName,
                   Environment.Is64BitProcess ? "bin\\x64" : "bin\\x86");

            //Initialize Wrapper for File API operations 
            MIP.Initialize(MipComponent.File, path);

            //Create ApplicationInfo, setting the clientID from Azure AD App Registration as the ApplicationId
            ApplicationInfo appInfo = new ApplicationInfo()
            {
                ApplicationId = clientId,
                ApplicationName = appName,
                ApplicationVersion = appVersion
            };

            //Instatiate the AuthDelegateImpl object, passing in AppInfo.
            AuthDelegateImplementation authDelegate = new AuthDelegateImplementation(appInfo, tenantId);

            //Initialize and instantiate the File Profile
            //Create the FileProfileSettings object
            var profileSettings = new FileProfileSettings(mipPath, false, authDelegate, new ConsentDelegateImplementation(), appInfo, LogLevel.Trace);

            //Load the Profile async and wait for the result
            var fileProfile = Task.Run(async () => await MIP.LoadFileProfileAsync(profileSettings)).Result;

            //Create a FileEngineSettings object, then use that to add an engine to the profile
            var engineSettings = new FileEngineSettings(name, "", "en-US");
            engineSettings.Identity = new Identity(name);

            var fileEngine = Task.Run(async () => await fileProfile.AddEngineAsync(engineSettings)).Result;

            // Just a test code to check if all the custom labels are populated or not
            foreach (var label in fileEngine.SensitivityLabels)
            {
                string labelName = label.Name;
            }

            //Set paths and label ID. You can paas file input stream too
            string filePath = Server.MapPath(@"~/App_Data/DemoAIPDoc.docx");
            string outputFilePath = @"D:\Test2.docx";

            var stream = System.IO.File.OpenRead(filePath);

            //Create a file handler for that file
            //Note: the 2nd inputFilePath is used to provide a human-readable content identifier for admin auditing.
            var handler = Task.Run(async () => await fileEngine.CreateFileHandlerAsync(stream, filePath, true)).Result;

            LabelingOptions labelingOptions = new LabelingOptions()
            {
                AssignmentMethod = AssignmentMethod.Privileged, //important as we are removing the label
                IsDowngradeJustified = true,
                JustificationMessage = "Lowering label"
            };

            //remove the label
            handler.DeleteLabel(labelingOptions);

            // Commit changes, save as outputFilePath.  You can also generate output stream
            var result = Task.Run(async () => await handler.CommitAsync(outputFilePath)).Result;

            return RedirectToAction("Index");
        }

        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }
    }
}