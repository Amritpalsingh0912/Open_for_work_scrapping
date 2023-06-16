using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Open_for_work_scrapping
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var configuration = new ConfigurationBuilder()
                  .SetBasePath(Directory.GetCurrentDirectory())
                  .AddJsonFile("appsettings.json")
                  .Build();

            string username = configuration.GetValue<string>("Username")!;
            string password = configuration.GetValue<string>("Password")!;
            string myNetwork = configuration.GetValue<string>("MyNetwork")!;
            string[] proxyIPAddresses = configuration.GetSection("ProxyIPAddresses").Get<string[]>();

            var proxy = new Proxy();
            proxy.IsAutoDetect = false;
            IWebDriver driver;
            proxy.HttpProxy = proxyIPAddresses[new Random().Next(0, proxyIPAddresses.Length)];
            ChromeOptions options = new ChromeOptions();
            options.AddArgument("--headless");
            options.Proxy = proxy;
            driver = new ChromeDriver(options);
            driver.Navigate().GoToUrl("https://www.linkedin.com/login");
            driver.FindElement(By.Id("username")).SendKeys(username);
            driver.FindElement(By.Id("password")).SendKeys(password);
            driver.FindElement(By.XPath("//button[@type='submit']")).Click();
            driver.Navigate().GoToUrl(myNetwork);
            System.Threading.Thread.Sleep(3000);

            List<ProfileData> profiles = new List<ProfileData>();
            int profileIndex = 0;
            bool hasMoreProfiles = true;

            while (hasMoreProfiles)
            {
                System.Threading.Thread.Sleep(2000);
                var profileElements = driver.FindElements(By.CssSelector(".mn-connection-card__occupation.t-14.t-black--light.t-normal"));


                if (profileIndex >= profileElements.Count)
                {
                    hasMoreProfiles = false;
                    Console.WriteLine("All profiles have been scraped.");
                }
                else
                {
                    profileElements[profileIndex].Click();
                    System.Threading.Thread.Sleep(2000);
                    ProfileData profileData = new ProfileData();
                    profileData.ProfilePicUrl = FindProfilePictureUrl(driver);
                    profileData.BackgroundCoverImageUrl = FindBackgroundCoverImageUrl(driver);
                    profileData.FullName = FindElementText(driver, By.CssSelector(".text-heading-xlarge.inline.t-24.v-align-middle.break-words"));
                    profileData.FirstName = ExtractFirstName(profileData.FullName);
                    profileData.MiddleName = ExtractMiddleName(profileData.FullName);
                    profileData.LastName = ExtractLastName(profileData.FullName);
                    profileData.Headline = FindElementText(driver, By.CssSelector(".text-body-medium.break-words"));
                    profileData.FullAddress = FindElementText(driver, By.CssSelector(".text-body-small.inline.t-black--light.break-words"));
                    profiles.Add(profileData);
                    Console.WriteLine("Profile Picture URL: " + profileData.ProfilePicUrl);
                    Console.WriteLine("Background Cover Image URL: " + profileData.BackgroundCoverImageUrl);
                    Console.WriteLine("Full Name: " + profileData.FullName);
                    Console.WriteLine("First Name: " + profileData.FirstName);
                    Console.WriteLine("Middle Name: " + profileData.MiddleName);
                    Console.WriteLine("Last Name: " + profileData.LastName);
                    Console.WriteLine("Headline: " + profileData.Headline);
                    Console.WriteLine("Full Address: " + profileData.FullAddress);
                    driver.Navigate().Back();
                    profileIndex++;
                }
            }

            string json = JsonConvert.SerializeObject(profiles, Formatting.Indented);
            File.WriteAllText("profiles.txt", json);            
            string currentDirectory = Directory.GetCurrentDirectory();
            string parentDirectory = Directory.GetParent(currentDirectory).Parent?.Parent?.Parent?.FullName;           
            string downloadPath = Path.Combine(parentDirectory, "profiles.txt");
            File.Copy("profiles.txt", downloadPath, true);
            Console.WriteLine("Downloaded JSON file to: " + downloadPath);

            driver.Close();
            driver.Quit();
        }
        static string FindProfilePictureUrl(IWebDriver driver)
        {
            try
            {
                IWebElement profilePictureElement = driver.FindElement(By.CssSelector(".pv-top-card-profile-picture__image.pv-top-card-profile-picture__image--show.evi-image.ember-view"));
                return profilePictureElement.GetAttribute("src");
            }
            catch (NoSuchElementException)
            {
                Console.WriteLine("Profile picture not found.");
                return null!;
            }
        }

        static string FindBackgroundCoverImageUrl(IWebDriver driver)
        {
            try
            {
                IWebElement backgroundCoverImageElement = driver.FindElement(By.CssSelector(".profile-background-image__image.relative.full-width.full-height"));
                return backgroundCoverImageElement.GetAttribute("src");
            }
            catch (NoSuchElementException)
            {
                Console.WriteLine("Background cover image not found.");
                return null!;
            }
        }

        static string FindElementText(IWebDriver driver, By locator)
        {
            try
            {
                IWebElement element = driver.FindElement(locator);
                return element.Text;
            }
            catch (NoSuchElementException)
            {
                Console.WriteLine("Element not found: " + locator.ToString());
                return null!;
            }
        }

        static string ExtractFirstName(string fullName)
        {
            if (string.IsNullOrEmpty(fullName))
                return string.Empty;

            string[] nameParts = fullName.Split(' ');
            return nameParts[0];
        }

        static string ExtractMiddleName(string fullName)
        {
            if (string.IsNullOrEmpty(fullName))
                return string.Empty;

            string[] nameParts = fullName.Split(' ');
            if (nameParts.Length > 2)
                return nameParts[1];

            return string.Empty;
        }

        static string ExtractLastName(string fullName)
        {
            if (string.IsNullOrEmpty(fullName))
                return string.Empty;

            string[] nameParts = fullName.Split(' ');
            if (nameParts.Length > 1)
                return nameParts[nameParts.Length - 1];

            return string.Empty;
        }

        
    }

    class ProfileData
    {
        public string ProfilePicUrl { get; set; } = string.Empty;
        public string BackgroundCoverImageUrl { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string MiddleName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Headline { get; set; } = string.Empty;
        public string FullAddress { get; set; } = string.Empty;
       
    }

   
}
