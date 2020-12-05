using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net;
using System.Threading;

namespace cURL_Try1
{
    class Program
    {
        static void Main(string[] args)
        {
            string kortlinkBase = "http://kortlink.dk/";
            string slashUrl = "";
            List<char> asciiList = new List<char>();
            List<int> urlList = new List<int>() { 10, 10, 10 }; //Default start value
             

            Console.WriteLine("Starting (Expect errors, just calling it now)");
            //CREATING THE ID ASCII LIST
            Console.WriteLine("Constructing ASCII list");
            for (int i = 48; i <= 109; i++)
            {
                //48 - 57 / 65 - 90 / 97 - 122
                int asciiValue = i;
                if (asciiValue > 57)
                {
                    asciiValue += +7;
                    if (asciiValue > 90)
                    {
                        asciiValue += 6;
                    }
                }
                char c = Convert.ToChar(asciiValue);
                asciiList.Add(c);
                Console.Write(c);
            }
            Console.WriteLine("\nAscii list count = " + asciiList.Count);
            Console.WriteLine("\n");



            int latestId = DatabaseEngine.GetTabelMaxID();
            string latestEntry = "AAA";
            if (!(latestId == -1)) //So basically if the database is empty is will start at AAA
            {
                latestEntry = DatabaseEngine.GetKortlinkById(latestId);
                Console.Write("Latest kortlink = " + latestEntry);
                latestEntry = latestEntry.Replace("http://kortlink.dk/", "");
                Console.WriteLine(" | Setting offset at = " + latestEntry);
            }

            int difference = latestEntry.Length - urlList.Count;
            Console.WriteLine("Difference = " + difference);
            for(int i = 0; i < difference; i++)
            {
                urlList.Add(10);
            }

            Console.WriteLine("Url List: ");
            for (int i = 0; i <= latestEntry.Length - 1; i++)
            {
                urlList[i] = asciiList.IndexOf(latestEntry[i]);
                Console.Write(urlList[i] + ", ");
            }

            if(latestId == -1)
            {
                urlList[urlList.Count - 1]--;
            }
            Console.WriteLine("\n\n");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Press ENTER to start bruteforcing");
            Console.WriteLine("To pause the program press any key");
            Console.ResetColor();
            Console.ReadLine();


            //BRUTE FORCING BEGINS HERE
            while (true)
            {
                //asciiList[asciiList.Count-1])

                bool levelUp = true;
                foreach(int numb in urlList)
                {
                    if( !(numb == asciiList[asciiList.Count - 1]) ){
                        levelUp = false;
                        break;
                    }
                }
                if (levelUp)
                {
                    urlList.Add(10);
                }



                urlList[urlList.Count - 1]++; //Add 1 til urlList, altså index (den sidste)

                for (int ii = urlList.Count - 1; ii >= 0; ii--) //Sørg for at alle elementer 
                {                                               //ikke er over 62 som er i asciilisten
                    if (urlList[ii] >= asciiList.Count)
                    {
                        if(ii == 0)
                        {
                            urlList.Insert(0, 10);
                        }
                        urlList[ii] = 0;
                        urlList[ii - 1]++;
                    }
                }
                
                slashUrl = "";
                foreach (int intyBoi in urlList)
                {
                    slashUrl += asciiList[intyBoi];
                }

                string kortlinkUrl = kortlinkBase + slashUrl;
                //Console.WriteLine("Status code = " + myWebRes.StatusCode);

                string redirectLocation = "";
                HttpWebResponse myWebRes;
                myWebRes = GetPage(kortlinkUrl);

                int whileCount = 0;
                try
                {
                    while (whileCount < 10)
                    {
                        redirectLocation = myWebRes.Headers["Location"];

                        if (redirectLocation.Contains("http://kortlink.dk/") || redirectLocation.Equals("http://kortlink.dk/"))
                        {
                            myWebRes = GetPage(redirectLocation);
                        }
                        else
                        {
                            break;
                        }
                        whileCount++;
                    }

                    if (!redirectLocation.Equals(""))
                    {
                        Console.WriteLine(kortlinkUrl + ": Redirect location = " + redirectLocation);
                    }
                }
                catch
                {
                    Console.WriteLine("Redirect Location was null");
                }
                //SQL add it to the database
                DatabaseEngine.InsertLinkPair(kortlinkUrl, redirectLocation);


                if (Console.KeyAvailable)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("\n\nKey pressed. Program paused");
                    Console.WriteLine("Press enter to continue the program");
                    Console.ReadLine();
                    Console.ResetColor();
                }

            }
        }












        public static HttpWebResponse GetPage(String url)
        {
            try
            {
                Uri ourUri = new Uri(url);
                // Creates an HttpWebRequest for the specified URL.
                HttpWebRequest myHttpWebRequest = (HttpWebRequest)WebRequest.Create(ourUri);
                myHttpWebRequest.AllowAutoRedirect = false;
                HttpWebResponse myHttpWebResponse = (HttpWebResponse)myHttpWebRequest.GetResponse();
                //Console.WriteLine("\nThe server did not issue any challenge");
                // Releases the resources of the response.
                myHttpWebResponse.Close();
                return myHttpWebResponse;
            }
            catch (WebException e)
            {
                HttpWebResponse response = (HttpWebResponse)e.Response;
                if (response != null)
                {
                    if (response.StatusCode == HttpStatusCode.Unauthorized)
                    {
                        string challenge = null;
                        challenge = response.GetResponseHeader("WWW-Authenticate");
                        if (challenge != null)
                            Console.WriteLine("\nThe following challenge was raised by the server:{0}", challenge);
                    }
                    else
                        Console.WriteLine("\nThe following WebException was raised : {0}", e.Message);
                }
                else
                    Console.WriteLine("\nResponse Received from server was null");

                return response;
            }
        }
    }

















    public static class DatabaseEngine
    {
        public static List<kortlinkDatabase> SearchKeyword(string keyword)
        {
            List<kortlinkDatabase> returnList = new List<kortlinkDatabase>();
            using (tmnfGAEntities entiTables = new tmnfGAEntities())
            {
                returnList = (from p in entiTables.kortlinkDatabases
                              where p.redirectLocation.Contains(keyword)
                              select p).ToList();
            }
            return returnList;
        }

        public static bool InsertLinkPair(string kortlink, string redirectLink)
        {
            using (tmnfGAEntities entiTables = new tmnfGAEntities())
            {

                kortlinkDatabase entry = new kortlinkDatabase();
                entry.kortlink = kortlink;
                entry.redirectLocation = redirectLink;

                try
                {
                    entiTables.kortlinkDatabases.Add(entry);
                    entiTables.SaveChanges();
                }
                catch
                {
                    Console.WriteLine("Error inserting into database");
                    return false;
                }
            }
            return true;
        }


        public static int GetTabelMaxID()
        {
            int maxId = -1; //Error if -1
            using (tmnfGAEntities entities = new tmnfGAEntities())
            {
                try
                {
                    maxId = entities.kortlinkDatabases.Max(p => p.id);
                }
                catch
                {
                    Console.WriteLine("Error with max");
                }

                Console.WriteLine("MAX ID = " + maxId);

                return maxId;
            }
        }

        public static string GetKortlinkById(int getId)
        {
            List<kortlinkDatabase> kLink = new List<kortlinkDatabase>();

            using (tmnfGAEntities entities = new tmnfGAEntities())
            {
                kLink = (from p in entities.kortlinkDatabases
                         where p.id == getId
                         orderby p.id descending
                         select p).ToList();
            }

            return kLink[0].kortlink;
        }

    }
}