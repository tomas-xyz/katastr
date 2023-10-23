using Newtonsoft.Json;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using System.IO.Compression;
using System.Text.RegularExpressions;

namespace tomxyz.katastr;

public class KatastrDownloaded
{
    private static string ConfigFileName = "katastr.json";
    private static string LoginPage = "https://login.cuzk.cz/login.do?typPrihlaseni=NAHLIZENI";
    private static string FlatPage = "https://nahlizenidokn.cuzk.cz/VyberBudovu/Jednotka/InformaceO";
    private static string LoginButtonId = "niaSubmitBtn";
    private static string AddressInputID = "ctl00_bodyPlaceHolder_txtAdresa";
    private static string MobileKeyClass = "gg-idprecord-link";
    private static string AfterLoginElementClass = "rychlaNavigace-box";
    private static string AfterAddressElementId = "ctl00_bodyPlaceHolder_panelSeznamBudov";
    private static string FlatsTableClass = "zarovnat stinuj  ";
    private static string PlombClass = "plomba";
    private static TimeSpan WaitingTimeout = TimeSpan.FromMinutes(5);
    private static string ChromeDriverBinary = "chromedriver.exe";
    private static string ChromeDriverVersionUrl = "https://googlechromelabs.github.io/chrome-for-testing/latest-versions-per-milestone-with-downloads.json";

    public static int Main(string[] args)
    {
        try
        {
            var configuration = new Configuration();
            try
            {
                var file = File.ReadAllText(ConfigFileName);
                configuration = JsonConvert.DeserializeObject<Configuration>(file);
                if (configuration == null)
                    throw new Exception($"Konfigurační soubor '{ConfigFileName}' se nebylo možné zpracovat");
            }
            catch (Exception e)
            {
                throw new Exception($"Konfigurační soubor '{ConfigFileName}' neexistuje nebo se jej nepodařilo zpracovat", e);
            }

            IWebDriver driver = null;
            try
            {
                driver = new ChromeDriver();
            }
            catch (InvalidOperationException e) when (e.Message.Contains("ChromeDriver only supports Chrome version"))
            {
                var version = Regex.Match(e.Message, ".*Current browser version is (.*) with binary path");
                if (version.Success)
                {
                    if (!DownloadChromeDriver(version.Groups[1].Value))
                    {
                        throw;
                    }

                    driver = new ChromeDriver();
                }
                else
                {
                    throw;
                }
            }

            driver.Navigate().GoToUrl(LoginPage);

            // login page
            var login = driver.FindElement(By.Id(LoginButtonId));
            login.Click();

            if (configuration.mobilniKlic)
            {
                var mobKey = driver.FindElement(By.ClassName(MobileKeyClass));
                mobKey.Click();
            }

            // wait for login
            if (!driver.WaitFor(WaitingTimeout, By.ClassName(AfterLoginElementClass)))
                throw new Exception($"Prihlaseni se nezdarilo v casovem limitu '{WaitingTimeout}'");

            driver.Navigate().GoToUrl(FlatPage);
            driver.FindElement(By.Id(AddressInputID)).SendKeys(configuration.adresa);
            driver.WaitFor(TimeSpan.FromMinutes(1), By.Id(AfterAddressElementId));

            var elements = driver.FindElements(By.XPath($"//table[@class = '{FlatsTableClass}']//td/a"));
            var links = elements.Select(x => x.GetAttribute("href"));

            var currentTab = driver.WindowHandles.FirstOrDefault();

            var tabs = new List<string>();
            foreach (var link in links)
            {// open all flat units and check plombs
                var tab = driver.NewTab(link);
                driver.SwitchTo().Window(tab);

                try
                {
                    var plomba = driver.FindElement(By.ClassName(PlombClass));
                    if (plomba == null)
                        driver.Close();
                }
                catch (Exception)
                {
                    driver.Close();
                }

                driver.SwitchTo().Window(currentTab);
            }

            return 0;
        }
        catch (InvalidOperationException e) when (e.Message.Contains("ChromeDriver only supports Chrome version"))
        {
            Console.WriteLine($"\r\nVas ChromeDriver.exe neodpovida verzi prohlizece. Stahnete si aktualni ze stranek 'https://chromedriver.chromium.org/downloads' a soubor nahradte. \r\n\r\n{e}");
            return 1;
        }
        catch (Exception e)
        {
            Console.WriteLine($"Nastala chyba: \r\n{e}");
            return 1;
        }
    }

    private static bool DownloadChromeDriver(string version)
    {
        Console.WriteLine($"Zkousim stahnout chrome driver pro prohlizec verze '{version}'...");

        using var httpClient = new HttpClient();
        var json = httpClient.GetStringAsync(ChromeDriverVersionUrl).GetAwaiter().GetResult();

        var versions = version.Split('.');
        dynamic platforms = JsonConvert.DeserializeObject<dynamic>(json)["milestones"][versions[0]]["downloads"]["chromedriver"];

        var url = string.Empty;
        foreach (var platform in platforms)
        {
            if (platform["platform"] == "win64")
            {
                url = platform["url"];
                break;
            }
        }

        if (string.IsNullOrEmpty(url))
        {
            Console.WriteLine($"Chrome driver pro prohlizec verze '{version}' se nepodařilo stáhnout...");
            return false;
        }

        Console.WriteLine($"Stahuji Chrome driver pro prohlizec verze '{version}' z url '{url}'...");

        using var response = httpClient.GetAsync(url).GetAwaiter().GetResult();
        using var downloadtream = response.Content.ReadAsStreamAsync().GetAwaiter().GetResult();

        var dirName = $"chromeDriver-{version}";
        var fileName = $"{dirName}.zip";

        try
        {
            // clear old dir or file
            try
            {
                File.Delete(fileName);
                Directory.Delete(dirName, true);
            }
            catch { }

            {
                using var fileStream = File.Create(fileName);
                downloadtream.CopyTo(fileStream);
            }

            Console.WriteLine($"Rozbaluji Chrome driver...");
            ZipFile.ExtractToDirectory(fileName, dirName);
            var target = Directory.GetDirectories(dirName)[0] + $"\\{ChromeDriverBinary}";

            // move to current dir with override the current version
            try
            {
                File.Move(ChromeDriverBinary, $"{ChromeDriverBinary}_old", true); // rename orig
            }
            catch { }

            File.Move(target, ChromeDriverBinary, true);

            Console.WriteLine($"Chrome driver stazen a nahrazen");
        }
        catch (Exception e)
        {
            int a = 1;
        }
        finally
        {
            try
            {
                File.Delete(fileName);
                Directory.Delete(dirName, true);
            }
            catch { }
        }

        return true;
    }
}
