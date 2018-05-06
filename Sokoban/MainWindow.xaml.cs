using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace Sokoban
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private static CookieContainer CreateCookieContainer(string cookies, out string userId, out string sessionId)
        {
            userId = "";
            sessionId = "";
            var domain = new Uri("https://logic-games.spb.ru");
            var cookieContainer = new CookieContainer();

            var parts = cookies.Replace(" ", "").Split(';');

            for (var i = 0; i < parts.Length; i++)
            {
                var subParts = parts[i].Split('=');

                cookieContainer.Add(new Cookie(subParts[0], subParts[1]) { Domain = domain.Host });

                if (subParts[0] == "userId")
                {
                    userId = subParts[1];
                }
                else if (subParts[0] == "sessionId")
                {
                    sessionId = subParts[1];
                }
            }

            return cookieContainer;
        }

        private async Task<string> Load(string cookies, int gameId)
        {
            string userId, sessionId;
            var cookieContainer = CreateCookieContainer(cookies, out userId, out sessionId);

            using (var handler = new HttpClientHandler() { CookieContainer = cookieContainer, AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate })
            using (var client = new HttpClient(handler))
            {
                client.BaseAddress = new Uri("https://logic-games.spb.ru");
                client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/65.0.3325.181 Safari/537.36 OPR/52.0.2871.40");
                client.DefaultRequestHeaders.AcceptEncoding.Add(new System.Net.Http.Headers.StringWithQualityHeaderValue("gzip"));
                client.DefaultRequestHeaders.AcceptEncoding.Add(new System.Net.Http.Headers.StringWithQualityHeaderValue("deflate"));
                client.DefaultRequestHeaders.AcceptEncoding.Add(new System.Net.Http.Headers.StringWithQualityHeaderValue("br"));
                client.DefaultRequestHeaders.AcceptLanguage.Add(new System.Net.Http.Headers.StringWithQualityHeaderValue("en-US", 0.9));
                client.DefaultRequestHeaders.AcceptLanguage.Add(new System.Net.Http.Headers.StringWithQualityHeaderValue("en", 0.9));
                client.DefaultRequestHeaders.Referrer = new Uri("https://logic-games.spb.ru/sokoban/");

                var dict = new Dictionary<string, string>();
                dict.Add("newGameId", gameId.ToString());
                dict.Add("oldGameId", "-1");
                dict.Add("settings", "%7B%22playMode%22%3A1%2C%22playFilter%22%3A0%2C%22playWithinCollection%22%3Afalse%2C%22playMobile%22%3Afalse%2C%22switchBoxes%22%3Afalse%7D");
                dict.Add("sessionId", sessionId);
                dict.Add("userId", userId);
                dict.Add("gameVariationId", "7");

                var requestContent = new FormUrlEncodedContent(dict);
                var response = await client.PostAsync("/sokoban/gw/loadGame.php", requestContent);

                return await response.Content.ReadAsStringAsync(); 
            }
        }

        private string[] levels = new[] { Solver.Sokoban.Level1,
                                          Solver.Sokoban.Level2,
                                          Solver.Sokoban.Level10,
                                          Solver.Sokoban.Level13,
                                          Solver.Sokoban.Level18,
                                          Solver.Sokoban.Level22,
                                          Solver.Sokoban.Level27,
                                          Solver.Sokoban.Level39};
        private int currentLevel = 0;

        private void OnNextButton_Click(object sender, RoutedEventArgs e)
        {
            currentLevel++;
            if (currentLevel >= levels.Length)
            {
                currentLevel = 0;
            }
            managerControl.Manager = Network.Manager.Using(levels[currentLevel]);
        }

        private void OnShowHideButton_Click(object sender, RoutedEventArgs e)
        {
            if (managerControl.Manager != null)
            {
                managerControl.Manager.ShowHideBoxes();
            }
        }

        private async void OnStartButton_Click(object sender, RoutedEventArgs e)
        {
            //var navigator = Solver.Navigator.Using(Solver.Sokoban.Level10Data);
            //var locationGroups = Solver.Graph.GetLocationGroups(navigator);
            //var order = Graph.GetFillingOrder(navigator, locationGroups.Values);
            //var anyGroup = locationGroups.First().Value;
            //var steps = anyGroup.GetFillingSteps(navigator);
            //navigator = navigator.ReplaceWithBoxes(anyGroup);

            //navigatorVisualizer.Navigator = navigator;
            //managerControl.Manager = Network.Manager.Using(Solver.Sokoban.Level10Data);
            managerControl.Manager = Network.Manager.Using(levels[currentLevel]);

            //var response = await Load(cookies.Text, 18);
            //var responseData = JsonConvert.DeserializeObject<Response>(response);



            //var agent = new Solver.Agent(colorMapControl, responseData.Data.GameData);
            //await agent.SolveAsync();

            //var opera = Process.GetProcessesByName("opera").Where(x => x.MainWindowHandle != IntPtr.Zero)
            //                   .Where(x => !string.IsNullOrWhiteSpace(x.MainWindowTitle) && x.MainWindowTitle.StartsWith("«Сокобан»"))
            //                   .FirstOrDefault();

            //for (int gameId = 2; gameId < 30; gameId++)
            //{
            //    //await RefreshPage(opera);
            //    await PlayGame(gameId, opera);
            //}
        }

        //private async Task RefreshPage(Process process)
        //{
        //    await VirtualKeyboard.Send(process.MainWindowHandle, Key.F5);

        //    await Task.Delay(TimeSpan.FromSeconds(10));
        //}

        private async Task PlayGame(int gameId, Process process)
        {
            try
            {
                //var responseContent = await Load(cookies.Text, gameId);
                var responseContent = "{\"status\":\"ok\",\"data\":{\"gameId\":1,\"gameData\":\"11*19*00003333300000000000000300030000000000000032003000000000000333002330000000000030020203000000000333030330300033333330003033033333004433020020000000000443333330333031330044300003000003333333330000333333300000000\",\"attempts\":[{\"attemptId\":2925059,\"elapsed\":18580,\"history\":\"ulllllllruuurrluurdluulrdddlddd\",\"timestamp\":1522432752},{\"attemptId\":2925065,\"elapsed\":21758,\"history\":\"ullllruuulullddurrlllldddrrrrrrrrrrrrrllllllllllllll\",\"timestamp\":1522432779},{\"attemptId\":2925071,\"elapsed\":-68616,\"history\":\"ulllllrruuululldlldddrrrrrrrrrrrrrllllllllllrruuulullddduulldddrrrrrrrrrrrrdrulurldlllllllllluuullddlldrrrrrrrrrrrrrrurdldrlullllllluuullulddduulldddrrrrrrrrrrrrlllllllllluuuruuurddllddddrrruuullulddduulldddrrrrrrrrrrrurdldrlullllllllluuuruuulddddduulldddrrrrrrrrrrrdrulur\",\"timestamp\":1522432850},{\"attemptId\":2925079,\"elapsed\":129718,\"history\":\"ulllllrruuudddrddlllluuudlluuurruddduulldddrrrrrrrrrrrrrllllllllllllruuullddlldrrrrrrrrrrrrrrlllllllllluuuuruuldddddddurrrrrlddlllrl\",\"timestamp\":1522433005},{\"attemptId\":2925086,\"elapsed\":-168987,\"history\":\"ullllruuululldlldddrrrrrrrrrrrrrlllllllllllllulldrrrrrrrrrrrrrurlddrulurrdlllllllluuulullddduulldddrrrrrrrrrrrurdldrrllulllllluuullulddduulldddrrrrrrrrrrrrllllllluuulluuulddddduulldddrrrrrrrrrrrdrulurdllllllllruuulluuurddllddddrrruuullulddddudrrrrddlllludrrrruullruuullllldddrrrrrrrrrrrurdldr\",\"timestamp\":1522433177}],\"gameInfo\":{\"avgWinTime\":169234,\"totalPlayed\":190311,\"totalWon\":49523,\"label\":\"Thinking Rabbit Original \\/ Soko \\u21161\",\"bestWinTime\":0,\"userWinTime\":0,\"comment\":\"\",\"fav\":0,\"userName\":null}}}";
                //var responseContent = "{\"status\":\"ok\",\"data\":{\"gameId\":2,\"gameData\":\"10*14*33333333333300344003000003333440030200200334400323333003344000010330033440030300203333333303320203003020020202030030000300000300333333333333\",\"attempts\":[{\"attemptId\":2925101,\"elapsed\":-143710,\"history\":\"rdrrdddrruuluuruullulllldduurrrrdrrddldlllulllllluldurdrrrrdddldllurrdruuuurrdrrrdrddllllulllruuurrdrrruruullulllldduurrrrdrrddldlllullllllrrrrrrdrrruruullulldluldduurrrrdrrddldlllullllldlurulrdrrruuurrrrdlllulddurrrrrrddldlllullllldluurulrddrrruurrrrrrddlurulllllulddurrrrrrddldlllullllldluuurulrdddrrrddddlllurrdruuuddrrdrrrruululllullllldluuurddrrrrrdrrrdrddllulllldllurdruuuddrrrdrrruluulllullllldluudrrrrdddrrrdrrullllldllurdruuuddrrrrrrululllullllldludrurrrrrdrrdrdllllldllurdruuuddrrrruullullllldurrrrrdrdrdlllldllurdruuuddrrruululllluldrdl\",\"timestamp\":1522433556}],\"gameInfo\":{\"avgWinTime\":362727,\"totalPlayed\":90516,\"totalWon\":18146,\"label\":\"Thinking Rabbit Original \\/ Soko \\u21162\",\"bestWinTime\":0,\"userWinTime\":0,\"comment\":\"\",\"fav\":0,\"userName\":null}}}";
                //var responseContent = "{\"status\":\"ok\",\"data\":{\"gameId\":27,\"gameData\":\"13*19*0333333333333333330034440003000030003333444440023303032033444444300200300003344444430030030300333333333302002020030030000032332033233033000200003020000303003303330300332030302022000002002003030200002332033333303333333001033000000000000333333000000\",\"attempts\":[{\"attemptId\":2932583,\"elapsed\":60855,\"history\":\"lrlrlrlrlrllulurrlddrrruruururrr\",\"timestamp\":1522927014},{\"attemptId\":2935897,\"elapsed\":11688,\"history\":\"llulurrlldr\",\"timestamp\":1522927035}],\"gameInfo\":{\"avgWinTime\":790768,\"totalPlayed\":2805,\"totalWon\":1813,\"label\":\"Thinking Rabbit Original \\/ Soko \\u211627\",\"bestWinTime\":0,\"userWinTime\":0,\"comment\":\"\",\"fav\":0,\"userName\":null}}}";
                status.Text = responseContent;

                var moves = await Solve(responseContent);

                await Play(moves, process);

                status.Text = "Level " + gameId + " done";
            }
            catch (Exception error)
            {
                status.Text = error.Message;
            }
        }

        private async Task Play(List<Tuple<int, Key, int>> moves, Process process)
        {
            foreach (var key in moves)
            {
                await VirtualKeyboard.Send(process.MainWindowHandle, key);
                await Task.Delay(100);
            }
        }

        private async Task<List<Tuple<int, Key, int>>> Solve(string json)
        {
            var responseData = JsonConvert.DeserializeObject<Response>(json);
            //var gameData = new GameData(responseData.Data.GameData);

            var solver = new SokobanSolver(responseData.Data.GameData);

            return await solver.Solve();
        }
    }
}
