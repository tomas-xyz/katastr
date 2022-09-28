using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using System.Text.Json;

namespace tomxyz.katastr
{
    class KatastrDownloaded
    {
        private static string AddressFileName = "adresa.json";
        private static string LoginPage = "https://login.cuzk.cz/login.do?typPrihlaseni=NAHLIZENI";
        private static string FlatPage = "https://nahlizenidokn.cuzk.cz/VyberBudovu/Jednotka/InformaceO";
        private static string LoginButtonId = "niaSubmitBtn";
        private static string AddressInputID = "ctl00_bodyPlaceHolder_txtAdresa";
        private static string MobileKeyClass = "gg-idprecord-link";
        private static string AfterLoginElementClass = "rychlaNavigace-box";
        private static string AfterAddressElementId = "ctl00_bodyPlaceHolder_panelSeznamBudov";
        private static string FlatsTableClass = "zarovnat stinuj  ";
        private static string PlombClass = "plomba";
        private static TimeSpan WaitingTimeout = TimeSpan.FromMinutes(2);


        public static int Main(string[] args)
        {
            try
            {
                var address = string.Empty;
                if (args.Length > 0)
                {
                    address = args[0];
                }

                if (string.IsNullOrEmpty(address))
                {
                    try
                    {
                        using var file = File.OpenRead(AddressFileName);
                        var addressJson = JsonSerializer.Deserialize<Address>(file);
                        if (addressJson != null)
                            address = addressJson.adresa;
                    }
                    catch (Exception)
                    {
                    }
                }

                IWebDriver driver = new ChromeDriver();
                driver.Navigate().GoToUrl(LoginPage);

                var login = driver.FindElement(By.Id(LoginButtonId));
                login.Click();

                var mobKey = driver.FindElement(By.ClassName(MobileKeyClass));
                mobKey.Click();

                // wait for login with mobile key
                driver.WaitFor(WaitingTimeout, By.ClassName(AfterLoginElementClass));

                driver.Navigate().GoToUrl(FlatPage);
                driver.FindElement(By.Id(AddressInputID)).SendKeys(address);
                driver.WaitFor(TimeSpan.FromMinutes(1), By.Id(AfterAddressElementId));

                var elements = driver.FindElements(By.XPath($"//table[@class = '{FlatsTableClass}']//td/a"));
                var links = elements.Select(x => x.GetAttribute("href"));

                // open all links in separated tabs
                var tabs = new List<string>();
                foreach (var link in links)
                    tabs.Add(driver.NewTab(link));

                foreach (var tab in tabs)
                {// close tabs without any plombs
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
                }

                return 0;
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error: \r\n{e}");
                return 1;
            }
        }
    }
}
