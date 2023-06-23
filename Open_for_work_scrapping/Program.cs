using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using System;
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
            string[] proxyIPAddresses = configuration.GetSection("ProxyIPAddresses").Get<string[]>();

            var proxy = new Proxy();
            proxy.IsAutoDetect = false;
            IWebDriver driver;
            proxy.HttpProxy = proxyIPAddresses[new Random().Next(0, proxyIPAddresses.Length)];
            ChromeOptions options = new ChromeOptions();
            options.AddArgument("--headless");
            options.Proxy = proxy;
            driver = new ChromeDriver(@"D:\Download\chromedriver");

            driver.Navigate().GoToUrl("https://www.linkedin.com/login");

            // Login
            driver.FindElement(By.Id("username")).SendKeys(username);
            driver.FindElement(By.Id("password")).SendKeys(password);
            driver.FindElement(By.XPath("//button[@type='submit']")).Click();
            System.Threading.Thread.Sleep(3000);

            Console.WriteLine("Select an option:");
            Console.WriteLine("1. Scrape open to work profiles");
            Console.WriteLine("2. Scrape Company profiles");
            Console.WriteLine("3. Scrape network profiles");
            string option = Console.ReadLine()!;

            switch (option)
            {
                case "1":
                    string profileSearchUrl = configuration.GetValue<string>("ProfileSearchUrl")!;
                    await ScrapeProfiles(driver, profileSearchUrl);
                    break;
                case "2":
                    string company = configuration.GetValue<string>("Company")!;
                    await ScrapeProfiles(driver, company);
                    break;
                case "3":
                    string myNetworkUrl = configuration.GetValue<string>("MyNetwork")!;
                    await ScrappedNetworkProfile(driver, myNetworkUrl);
                    break;
                default:
                    Console.WriteLine("Invalid option selected.");
                    break;
            }

            driver.Close();
            driver.Quit();
        }


        static async Task ScrapeProfiles(IWebDriver driver, string url)
        {
            List<ProfileData> profilesdata = new List<ProfileData>();
            driver.Navigate().GoToUrl(url);
            System.Threading.Thread.Sleep(3000);

            int profilesToScrape = 9;
            int totalPage = 50;

            for (int j = 0; j < totalPage; j++)
            {
                for (int i = 0; i <= profilesToScrape; i++)
                {
                    System.Threading.Thread.Sleep(2000);
                    // Go to the next user's profile
                    driver.FindElements(By.CssSelector(".entity-result__primary-subtitle.t-14.t-black.t-normal"))[i].Click();
                    System.Threading.Thread.Sleep(2000);
                    ProfileData profileData = new ProfileData();
                    profileData.ProfilePicUrl = FindProfilePictureUrl(driver);
                    profileData.BackgroundCoverImageUrl = FindBackgroundCoverImageUrl(driver);
                    profileData.FullName = FindElementText(driver, By.CssSelector(".text-heading-xlarge.inline.t-24.v-align-middle.break-words"));
                    profileData.Headline = FindElementText(driver, By.CssSelector(".text-body-medium.break-words"));
                    profileData.FullAddress = FindElementText(driver, By.CssSelector(".text-body-small.inline.t-black--light.break-words"));
                    profilesdata.Add(profileData);
                    Console.WriteLine("Profile Picture URL: " + profileData.ProfilePicUrl);
                    Console.WriteLine("Background Cover Image URL: " + profileData.BackgroundCoverImageUrl);
                    Console.WriteLine("Full Name: " + profileData.FullName);
                    Console.WriteLine("Headline: " + profileData.Headline);
                    Console.WriteLine("Full Address: " + profileData.FullAddress);
                    Console.WriteLine();

                    // Go back to search results
                    driver.Navigate().Back();
                    System.Threading.Thread.Sleep(2000);
                }

                // Click on the next page button
                IWebElement nextButton = driver.FindElement(By.CssSelector(".artdeco-pagination__button--next"));
                if (nextButton.GetAttribute("disabled") == "true")
                {
                    break;
                }
                else
                {
                    nextButton.Click();
                    System.Threading.Thread.Sleep(2000);
                }
            }
            string json = JsonConvert.SerializeObject(profilesdata, Formatting.Indented);
            string timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
            string fileName = $"profilesdata_{timestamp}.txt";
            File.WriteAllText(fileName, json);
            string currentDirectory = Directory.GetCurrentDirectory();
            string parentDirectory = Directory.GetParent(currentDirectory).Parent?.Parent?.FullName;
            string downloadPath = Path.Combine(parentDirectory, fileName);
            File.Copy(fileName, downloadPath, true);
            Console.WriteLine("Downloaded JSON file to: " + downloadPath);
        }
        static async Task ScrappedNetworkProfile(IWebDriver driver, string url)
        {
            driver.Navigate().GoToUrl(url);
            List<ProfileData> profiles = new List<ProfileData>();
            System.Threading.Thread.Sleep(3000);
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
                    profileData.Headline = FindElementText(driver, By.CssSelector(".text-body-medium.break-words"));
                    profileData.FullAddress = FindElementText(driver, By.CssSelector(".text-body-small.inline.t-black--light.break-words"));
                    profiles.Add(profileData);
                    Console.WriteLine("Profile Picture URL: " + profileData.ProfilePicUrl);
                    Console.WriteLine("Background Cover Image URL: " + profileData.BackgroundCoverImageUrl);
                    Console.WriteLine("Full Name: " + profileData.FullName);
                    Console.WriteLine("Headline: " + profileData.Headline);
                    Console.WriteLine("Full Address: " + profileData.FullAddress);
                    Console.WriteLine();
                    driver.Navigate().Back();
                    profileIndex++;
                }
            }
            string json = JsonConvert.SerializeObject(profiles, Formatting.Indented);
            string timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
            string fileName = $"profiles_{timestamp}.txt";
            File.WriteAllText(fileName, json);
            string currentDirectory = Directory.GetCurrentDirectory();
            string parentDirectory = Directory.GetParent(currentDirectory).Parent?.Parent?.FullName;
            string downloadPath = Path.Combine(parentDirectory, fileName);
            File.Copy(fileName, downloadPath, true);
            Console.WriteLine("Downloaded JSON file to: " + downloadPath);
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
                IWebElement backgroundCoverImageElement = driver.FindElement(By.CssSelector(".profile-background-image.profile-background-image--default"));
                return backgroundCoverImageElement.GetAttribute("src");
            }
            catch (NoSuchElementException)
            {
                Console.WriteLine("Background cover image not found.");
                return null!;
            }
        }
        static string FindElementText(IWebDriver driver, By by)
        {
            try
            {
                return driver.FindElement(by).Text;
            }
            catch
            {
                return "Element not found.";
            }
        }


    }

    class ProfileData
    {
        public string ProfilePicUrl { get; set; } = string.Empty;
        public string BackgroundCoverImageUrl { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Headline { get; set; } = string.Empty;
        public string FullAddress { get; set; } = string.Empty;

    }
}

