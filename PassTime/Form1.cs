using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;

namespace PassTime
{
    public partial class Form1 : Form
    {
        //variables here↓
        int pY;  //determines verticle movement
        int pX;
        int jumpCap;
        int platformCount = 0;  //keeps track of current platform
        int spiderCount = 0;
        int busterCount = 0; //keeps track of buster onscreen
        int windCount = 0;
        int defaultStart = 400; //default height at which obstacles start + minimum height afterwards
        int score = 0;
        int tempScore; //used to check for highscore
        int[] tempY; //stores y value of bonus
        int[] amp;  //stores amplitude for bonus
        int[] peroid;
        bool stutterJump; //determines wheather player is currently falling
        int pic = 0;
        //-----------------------//
        Rectangle player;
        Rectangle[] buster; //rectangle for shots
        Rectangle[] platform;
        Rectangle[] spider;
        Rectangle[] wind;
        ProgressBar bar;
        Image playerPic;
        Image busterPic;
        Image bonusPic;
        Timer spawnTimer; //timer that spawns the platforms
        Timer refresh; //refreshes screen also checks for game over
        Timer moveTimer; //does almost everything involved to moving stuff
        Timer scoreTimer;
        Timer animatedRun;
        Label title;
        Button startBtn;
        Random rNum; //random number
        StreamWriter writeScore; //writes score to a txt file
        StreamReader readScore;

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            //Form size & stuff
            this.Size = new Size(600, 400);
            this.MaximumSize = new Size(600, 400);
            this.MinimumSize = new Size(600, 400);
            this.Location = new Point(300, 100);
            this.Text = "Jumper";
            this.BackColor = Color.BurlyWood;

            //Random number stuff
            rNum = new Random();

            bar = new ProgressBar();
            bar.Size = new System.Drawing.Size(100, 20);
            bar.Location = new Point(0, 0);
            bar.MarqueeAnimationSpeed = 500;
            bar.Minimum = 0;

            if (File.Exists(@"score.txt"))
            {
                readScore = new StreamReader(@"score.txt");
                tempScore = Convert.ToInt32(readScore.ReadLine());
            }
            readScore.Close();

            bar.Maximum = tempScore;
            bar.Value = 0;
            bar.Visible = true;

            //Start Button stuff
            startBtn = new Button();
            startBtn.Text = "Start";
            startBtn.BackColor = Color.White;
            startBtn.Size = new Size(80, 30);
            startBtn.Location = new Point((this.ClientSize.Width / 2) - (startBtn.Width / 2), (this.ClientSize.Height / 2) - (startBtn.Height / 2));
            startBtn.Visible = true;
            startBtn.Click += startBtn_Click;

            //Label things↓
            title = new Label();
            title.Font = new Font("Arial", 10);
            title.Text = "Instructions: Make MegaMan jump by pressing the up arrow Space will fire a buster shot, hit spiders for a bonus!";
            title.Size = new Size(400, 45);
            title.Location = new Point((this.ClientSize.Width / 2) - (title.Width / 2), (this.ClientSize.Height / 3));
            title.Visible = true;

            //setting up player rectangle stuff
            playerPic = Image.FromFile(@"MegaMan.png", true);
            player = new Rectangle();
            player.Size = new Size(playerPic.Width, playerPic.Height);

            busterPic = Image.FromFile(@"Buster.png", true);
            buster = new Rectangle[5];
            for (int i = 0; i < buster.Length; i++)
            {
                buster[i].Size = new Size(busterPic.Width, busterPic.Height);
                buster[i].Location = new Point(0, -400);
            }

            //bonus rectangle
            bonusPic = Image.FromFile(@"Bonus.png", true);
            spider = new Rectangle[3];
            amp = new int[spider.Length];
            peroid = new int[spider.Length];
            tempY = new int[spider.Length];
            for (int i = 0; i < spider.Length; i++)
            {
                spider[i].Size = new Size(bonusPic.Width, bonusPic.Height);
                spider[i].Location = new Point(0, -400);
            }

            //platform rectangle!
            platform = new Rectangle[7];
            for (int i = 0; i < platform.Length; i++)
            {
                platform[i].Size = new Size(120, 10);
                platform[i].Location = new Point(0, -400);
            }

            //background wind
            wind = new Rectangle[12];
            for (int i = 0; i < wind.Length; i++)
            {
                wind[i].Size = new Size(35, 3);
                wind[i].Location = new Point(0, -400);
            }

            //Timer things↓
            refresh = new Timer();
            refresh.Interval = (1000 / 60);
            refresh.Tick += refresh_Tick;

            spawnTimer = new Timer();
            spawnTimer.Interval = 1000;
            spawnTimer.Tick += spawnTimer_Tick;

            moveTimer = new Timer();
            moveTimer.Interval = (1000 / 60);
            moveTimer.Tick += moveTimer_Tick;
            moveTimer.Enabled = true;

            scoreTimer = new Timer();
            scoreTimer.Interval = 100;
            scoreTimer.Tick += scoreTimer_Tick;

            animatedRun = new Timer();
            animatedRun.Interval = 100;
            animatedRun.Tick += new EventHandler(animatedRun_Tick);

            //player movement
            this.KeyDown += Form1_KeyDown;
            this.KeyUp += Form1_KeyUp;

            //paint related stuff
            this.DoubleBuffered = true;

            //creates menu
            menu();
        }

        void animatedRun_Tick(object sender, EventArgs e)
        {
            if (false)
            {
                if (intersect())
                {
                    playerPic = Image.FromFile(@"MegaRun" + Convert.ToString(pic) + ".png", true);
                    pic++;

                    if (pic == 3)
                    {
                        pic = 0;
                    }
                }
            }
        }

        void scoreTimer_Tick(object sender, EventArgs e)
        { //upon ticking the score is increased and shown at the top
            score++;
            this.Text = "Score: " + score;
            bar.Step = 1;
            bar.PerformStep();
        }

        void moveTimer_Tick(object sender, EventArgs e)
        {
            //player moving
            if (pY > 0 && intersect())
            { //if player moves down and intersects with a platform(player stops on platform)...
                jumpCap = 0;
                stutterJump = false;
                playerPic = Image.FromFile(@"MegaMan.png", true);
            }
            else if (pY < 0)
            { //if player is moving up...
                player.Y += pY;
                jumpCap++;
            }
            else if (pY > 0 && !intersect())
            { //if player is moving down and not touching a platform...
                playerPic = Image.FromFile(@"MegaManJump.png", true);
                player.Y += pY;
                stutterJump = true;
            }

            if (jumpCap >= 30)
            { //when jumpcap hit 30...
                pY = 6;
            }

            if (player.Left > 0 && pX < 0 || player.Right < this.ClientSize.Width && pX > 0)
            {
                player.X += pX;
            }

            //bustershot movement
            for (int i = 0; i < buster.Length; i++)
            {
                if (buster[i].Y > -100 && buster[i].Left <= this.ClientSize.Width)
                {
                    buster[i].X += 7;
                    hitTest();
                }

                if (buster[i].X > this.ClientSize.Width)
                {
                    buster[i].Location = new Point(0, -400);
                }
            }

            if (buster[0].Y < -100)
            {
                busterCount = 0;
            }

            //platform moving
            for (int i = 0; i < platform.Length; i++)
            { //have each platform move left if...
                if (platform[i].Y >= 0)
                { //platform is on screen
                    platform[i].X += -7;
                }

                if (platform[i].Right <= 0)
                { //if platform reaches left edge move it off screen
                    platform[i].Location = new Point(0, -400);
                }
            }

            //spider moving
            for (int i = 0; i < spider.Length; i++)
            {
                if (spider[i].Y > -100)//-100 helps against bugs
                {
                    spider[i].X -= 4;
                    spider[i].Y = Convert.ToInt32((amp[i] * (Math.Sin((1.0 / peroid[i]) * (spider[i].X - amp[i])))) + tempY[i]);
                }

                if (spider[i].Right <= 0)
                {
                    spider[i].Location = new Point(0, -400);
                }
            }

            //wind effects
            for (int i = 0; i < wind.Length; i++)
            {
                if (wind[i].Y >= 0)
                {
                    wind[i].X -= 10;
                }
                if (wind[i].X <= 0)
                {
                    wind[i].Location = new Point(0, -400);
                }
            }
        }

        void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Up && !stutterJump && jumpCap != 30)
            { //on up arrow pressed while player is not falling...
                pY = -6;

                playerPic = Image.FromFile(@"MegaManJump.png", true);
            }

            if (e.KeyCode == Keys.Left)
            {
                pX = -4;
            }
            else if (e.KeyCode == Keys.Right)
            {
                pX = 4;
            }

            //=================================//
            if (e.KeyCode == Keys.Space && busterCount < buster.Length && buster[busterCount].Y < 0)
            {
                buster[busterCount].X = player.Right;
                buster[busterCount].Y = (player.Top + 10);
                busterCount++;
            }

            if (!scoreTimer.Enabled && e.KeyCode == Keys.Up)
            { //if the score timer is disabled...
                scoreTimer.Start();
                animatedRun.Start();
            }
        }

        void Form1_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Up)
            { //on up arrow released...
                stutterJump = true; //dissallow mid air jumping
                pY = 6;
            }

            if (e.KeyCode == Keys.Left || e.KeyCode == Keys.Right)
            {
                pX = 0;
            }
        }

        void startBtn_Click(object sender, EventArgs e)
        {
            //↓Sets up everything needed for the game screen↓
            this.Text = "Score: " + score;
            title.Text = String.Empty;
            title.Controls.Add(bar);
            title.Size = new System.Drawing.Size(bar.Width, bar.Height);
            title.Location = new Point(0,0);
            this.Controls.Remove(startBtn);
            this.Paint += Form1_Paint;

            //sets player location, vert. movement, stutterjump + jumpcap to default values
            playerPic = Image.FromFile(@"MegaMan.png", true);
            player.Location = new Point(100, this.ClientSize.Height - (player.Height * 2));
            pY = 0;
            stutterJump = false;
            jumpCap = 0;

            refresh.Start();
            spawnTimer.Start();
        }

        void refresh_Tick(object sender, EventArgs e)
        {
            //wind effects


            if (windCount >= (wind.Length - 1) && wind[0].Y < 0)
            {
                windCount = 0;
            }

            if (rNum.Next(100) < 4 && wind[windCount].Y < 0 && player.Top < this.ClientSize.Height)
            {
                wind[windCount].X = this.ClientSize.Width;
                wind[windCount].Y = rNum.Next(this.ClientSize.Height - wind[0].Height);

                for (int i = 0; i < wind.Length; i++)
                {
                    if (wind[windCount] != wind[i])
                    {
                        while (wind[windCount].IntersectsWith(wind[i]))
                        {
                            wind[windCount].Y = rNum.Next(this.ClientSize.Height - wind[0].Height);
                        }
                    }
                }
            }

            if (wind[windCount].Y >= 0 && windCount != (wind.Length - 1))
            {
                windCount++;
            }

            this.Invalidate();

            if (player.Top >= this.ClientSize.Height)
            { //on player falling off screen...
                gameOver();
                menu();
            }
        }

        void spawnTimer_Tick(object sender, EventArgs e)
        {
            if (platformCount >= (platform.Length - 1) && platform[0].Y < 0)
            { //if platformcount is at max(4) and the first platform is not in use...
                platformCount = 0;
            }

            if (platform[platformCount].Y < 0 && player.Top < this.ClientSize.Height)
            { //if current platform is not in use and player is on screen...
                heightCheck(); //keeps platforms on screen

                //bring current platform to right side and place at a random height
                platform[platformCount].X = this.ClientSize.Width;
                platform[platformCount].Y = rNum.Next((defaultStart - 160), (this.ClientSize.Height - platform[platformCount].Height));
                defaultStart = platform[platformCount].Bottom;
                //defualtstart ensures player can reach the next platform
            }

            if (platform[platformCount].Y >= 0 && platformCount != (platform.Length - 1))
            { //changes current platform to next one when used
                platformCount++;
            }

            //spawns the bonus'
            if (rNum.Next(10) < 3)
            {
                if (spiderCount == (spider.Length - 1) && spider[0].Y < 0)
                {
                    spiderCount = 0;
                }

                if (spider[spiderCount].Y < 0 && player.Top < this.ClientSize.Height)
                {
                    amp[spiderCount] = rNum.Next(20, 100);
                    peroid[spiderCount] = rNum.Next(20, 50);
                    spider[spiderCount].X = this.ClientSize.Width;
                    spider[spiderCount].Y = rNum.Next(amp[spiderCount], (this.ClientSize.Height - amp[spiderCount]));
                    tempY[spiderCount] = spider[spiderCount].Y;
                }

                if (spider[spiderCount].Y >= 0 && spiderCount != (spider.Length - 1))
                { //changes current bonus to next one when used
                    spiderCount++;
                }
            }
        }

        void Form1_Paint(object sender, PaintEventArgs e)
        {
            //draws images within rectangles

            e.Graphics.FillRectangles(Brushes.White, wind);

            e.Graphics.DrawImage(playerPic, player);

            e.Graphics.FillRectangles(Brushes.Black, platform);

            for (int i = 0; i < buster.Length; i++)
            {
                e.Graphics.DrawImage(busterPic, buster[i]); //buster length = platform length
            }

            for (int i = 0; i < spider.Length; i++)
            {
                e.Graphics.DrawImage(bonusPic, spider[i]);
            }
        }

        public void menu()
        {
            //does menu stuff
            this.Controls.Clear();
            this.Controls.Add(title);
            this.Controls.Add(startBtn);
        }

        public bool intersect()
        { //checks for intersection with any of the platforms
            for (int i = 0; i < platform.Length; i++)
            {
                if (player.IntersectsWith(platform[i]) && player.Bottom >= platform[i].Top && player.Bottom <= platform[i].Top + 6)
                {
                    return true;
                }
            }

            return false;
        }

        public void hitTest()
        {
            for (int i = 0; i < buster.Length; i++)
            {
                if (scoreTimer.Enabled && buster[i].Y >= -100 && buster[i].IntersectsWith(spider[0]))
                {
                    buster[i].Location = new Point(0, -400);
                    spider[0].Location = new Point(0, -400);
                    score += 100;
                    bar.Step = 100;
                    bar.PerformStep();
                    this.Text = "Score: " + score;
                }
                else if (scoreTimer.Enabled && buster[i].Y >= -100 && buster[i].IntersectsWith(spider[1]))
                {
                    buster[i].Location = new Point(0, -400);
                    spider[1].Location = new Point(0, -400);
                    score += 100;
                    bar.Step = 100;
                    bar.PerformStep();
                    this.Text = "Score: " + score;
                }
                else if (scoreTimer.Enabled && buster[i].Y >= -100 && buster[i].IntersectsWith(spider[2]))
                {
                    buster[i].Location = new Point(0, -400);
                    spider[2].Location = new Point(0, -400);
                    score += 100;
                    bar.Step = 100;
                    bar.PerformStep();
                    this.Text = "Score: " + score;
                }
            }
        }

        public void heightCheck()
        { //keeps platforms on screen
            if (defaultStart < 0)
            { //if the defaultstart value is less that 0(off screen)...
                defaultStart = 170;
            }
        }

        public void gameOver()
        {
            title.Controls.Remove(bar);
            bar.Value = 0;
            title.Size = new Size(400, 45);

            for (int i = 0; i < platform.Length; i++)
            {
                platform[i].Location = new Point(0, -400);
                
            }

            for (int i = 0; i < buster.Length; i++)
            {
                buster[i].Location = new Point(0, -400);
            }

            for (int i = 0; i < spider.Length; i++)
            {
                spider[i].Location = new Point(0, -400);
            }

            //checks score and saves it if it is a high score
            if (File.Exists(@"score.txt"))
            {
                readScore = new StreamReader(@"score.txt");
                tempScore = Convert.ToInt32(readScore.ReadLine());
            }
            readScore.Close();

            writeScore = new StreamWriter(@"score.txt", false);
            if (score > tempScore)
            {
                title.Text = "HighScore: " + score + "\nYour Score: " + score;
                writeScore.WriteLine(score);
                bar.Maximum = score;
            }
            writeScore.WriteLine(tempScore);
            title.Text = "HighScore: " + tempScore + "\nYour Score: " + score;
            writeScore.Close();
            score = 0;

            //allows for multiple tries
            startBtn.Text = "Retry?";
            title.Width = 150;
            title.Location = new Point((this.ClientSize.Width / 2) - (title.Width / 2), (this.ClientSize.Height / 3));
            scoreTimer.Stop();
            refresh.Stop();
            spawnTimer.Stop();
            animatedRun.Stop();
        }
    }
}