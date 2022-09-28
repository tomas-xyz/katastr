using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using System.Text.Json;

namespace tomxyz.katastr
{
    class KatastrDownloaded
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

        public static int Main(string[] args)
        {
            try
            {
                var configuration = new Configuration();
                try
                {
                    using var file = File.OpenRead(ConfigFileName);
                    configuration = JsonSerializer.Deserialize<Configuration>(file);
                    if (configuration == null)
                        throw new Exception($"Konfigurační soubor '{ConfigFileName}' se nebylo možné zpracovat");
                }
                catch (Exception e)
                {
                    throw new Exception($"Konfigurační soubor '{ConfigFileName}' neexistuje nebo se jej nepodařilo zpracovat", e);
                }

                IWebDriver driver = new ChromeDriver();
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
            catch (Exception e)
            {
                Console.WriteLine($"Nastala chyba: \r\n{e}");
                return 1;
            }
        }
    }
}
