using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace tomxyz.katastr
{
    internal static class WebDriverExtensions
    {
        /// <summary>
        /// Wait for some element
        /// </summary>
        /// <param name="driver">web driver</param>
        /// <param name="timeSpan">timout</param>
        /// <param name="toFind">element to find</param>
        /// <returns>true if element has been found</returns>
        internal static bool WaitFor(this IWebDriver driver, TimeSpan timeSpan, By toFind)
        {
            var wait = new WebDriverWait(driver, timeSpan);
            return wait.Until(condition =>
            {
                try
                {
                    var elementToBeDisplayed = driver.FindElement(toFind);
                    return elementToBeDisplayed.Displayed;
                }
                catch (StaleElementReferenceException)
                {
                    return false;
                }
                catch (NoSuchElementException)
                {
                    return false;
                }
            });
        }

        /// <summary>
        /// Open link in new tab
        /// </summary>
        /// <param name="driver">web driver</param>
        /// <param name="link">link to open</param>
        /// <returns>tab id</returns>
        internal static string NewTab(this IWebDriver driver, string link)
        {
            IJavaScriptExecutor js = (IJavaScriptExecutor)driver;
            js.ExecuteScript($"window.open(\"{link}\")");
            return driver.WindowHandles.Last();
        }
    }
}
