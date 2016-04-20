using System;
using System.IO;
using System.Net;
using System.Runtime.ExceptionServices;
using System.Security;
using System.Text;
using System.Threading;
using System.Web;
using System.Windows.Forms;
namespace WebtoonDownloader
{
    public partial class Form1 : Form
    {
        int n = 0, a = 0, progressbar_max = 0;
        public static Form1 _frm1;
        public static Semaphore _resourcePool = null;
        const int maximum_thread = 20;
        string name = "", loc = "D:\\";
        public Form1()
        {
            InitializeComponent();
            _frm1 = this;
        }
        public void newLoad(int num)
        {
            listBox1.Invoke((Action)delegate
            {
                listBox1.Items.Add(num + "화 다운로드 중");
                listBox1.Update();
            });
        }
        public void doneLoad(int num)
        {
            listBox1.Invoke((Action)delegate
            {
                listBox1.Items.Remove(num + "화 다운로드 중");
                listBox1.Update();
            });
            progressBar1.Invoke((Action)delegate
            {
                progressBar1.Increment(1);
                if(progressBar1.Value >= progressbar_max && !listBox1.Items.Contains("다운로드 완료!"))
                {
                    listBox1.Invoke((Action)delegate
                    {
                        listBox1.Items.Add("다운로드 완료!");
                        listBox1.Update();
                    });
                }
            });
        }
        static HtmlElement ElementsByClass(HtmlDocument doc, string className)
        {
            foreach (HtmlElement e in doc.All)
                if (e.GetAttribute("className") == className)
                    return e;
            return null;
        }
        private void button1_Click(object sender, EventArgs e)
        {
            WebBrowser wb = new WebBrowser();
            wb.DocumentCompleted += new WebBrowserDocumentCompletedEventHandler(wb_DocumentCompleted1);
            wb.AllowNavigation = true;
            listBox1.Items.Add(textBox1.Text + " 검색 중..");
            wb.Navigate("http://comic.naver.com/search.nhn?keyword=" + HttpUtility.UrlEncode(textBox1.Text));
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox1.Checked)
            {
                comboBox1.SelectedIndex = comboBox1.FindStringExact("1");
                comboBox2.SelectedIndex = comboBox1.FindStringExact(a.ToString());
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            folderBrowserDialog1.ShowDialog();
            loc = folderBrowserDialog1.SelectedPath;
        }

        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void button2_Click(object sender, EventArgs e)
        {
            if(n == 0)
            {
                MessageBox.Show("웹툰을 조회하세요!");
                return;
            }
            if (checkBox1.Checked)
            {
                comboBox1.SelectedIndex = comboBox1.FindStringExact("1");
                comboBox2.SelectedIndex = comboBox1.FindStringExact(a.ToString());
            }
            if(comboBox2.SelectedItem == null || comboBox2.SelectedItem == null)
            {
                MessageBox.Show("범위를 선택하세요!");
                return;
            }
            progressbar_max = progressBar1.Maximum = int.Parse(comboBox2.SelectedItem.ToString()) - int.Parse(comboBox1.SelectedItem.ToString());
            progressBar1.Value = 0;
            for (int i = int.Parse(comboBox2.SelectedItem.ToString()); i >= int.Parse(comboBox1.SelectedItem.ToString()); i--)
            {
                new DownloadTask("http://comic.naver.com/webtoon/detail.nhn?titleId=" + n + "&no=" + i, i, n, name, loc, checkBox2.Checked);
            }
            _resourcePool.Release(maximum_thread);
        }

        private void wb_DocumentCompleted2(object sender, WebBrowserDocumentCompletedEventArgs e)
        {

            WebBrowser wb1 = sender as WebBrowser;
            HtmlDocument hd = wb1.Document;
            HtmlElement he = ElementsByClass(hd, "title");
            if (e.Url.AbsoluteUri != wb1.Url.AbsoluteUri)
            {
                return;
            }
            if (he == null)
            {
                MessageBox.Show("매칭되는게 없음");
                return;
            }
            HtmlElementCollection trs = he.GetElementsByTagName("a");
            string s = trs[0].GetAttribute("href");
            if (!s.Contains("webtoon"))
            {
                MessageBox.Show("매칭되는게 없음");
                return;
            }
            int tmp = s.IndexOf("no=") + 3;
            int tmp2 = tmp;
            while (true)
            {
                if ('0' <= s[tmp] && s[tmp] <= '9')
                {
                    tmp++;
                }
                else break;
            }
            a = int.Parse(s.Substring(tmp2, tmp - tmp2));
            if (_resourcePool == null)
                _resourcePool = new Semaphore(0, maximum_thread);
            for (int i = 1; i <= a; i++)
            {
                comboBox1.Items.Add(i);
                comboBox2.Items.Add(i);
            }
        }

        private void wb_DocumentCompleted1(object sender, WebBrowserDocumentCompletedEventArgs e)
        {
            WebBrowser wb = sender as WebBrowser;
            HtmlDocument hd = wb.Document;
            HtmlElement he = ElementsByClass(hd, "resultList");
            if (e.Url.AbsoluteUri != wb.Url.AbsoluteUri)
            {
                return;
            }
            listBox1.Items.Add(textBox1.Text + " 검색 성공!");
            if (he == null)
            {
                MessageBox.Show("매칭되는게 없음");
                return;
            }
            HtmlElementCollection trs = he.GetElementsByTagName("a");
            string s = trs[0].GetAttribute("href");
            if (!s.Contains("webtoon"))
            {
                MessageBox.Show("매칭되는게 없음");
                return;
            }
            name = trs[0].InnerText;
            MessageBox.Show(trs[0].InnerText+"이(가) 검색되었습니다!");
            n = int.Parse(s.Substring(s.IndexOf("titleId=") + 8));
            WebBrowser wb1 = new WebBrowser();

            wb1.DocumentCompleted += new WebBrowserDocumentCompletedEventHandler(wb_DocumentCompleted2);
            wb1.Navigate("http://comic.naver.com/webtoon/list.nhn?titleId=" + n);
        }
    }

    internal class DownloadTask
    {
        private int i;
        private string v;
        private int num;
        private string name;
        private string loc;
        private Boolean html_chk = false;

        public DownloadTask(string v, int i, int num, string name, string loc, Boolean html_chk)
        {
            this.v = v;
            this.i = i;
            this.num = num;
            this.name = name;
            this.loc = loc;
            this.html_chk = html_chk;

            ThreadStart starter = threadJob;
            starter += () =>
            {
                Form1._frm1.doneLoad(i);
            };
            Thread thread = new Thread(starter) { IsBackground = true };
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
        }
        private void threadJob()
        {
            Form1._resourcePool.WaitOne();
            Form1._frm1.newLoad(i);
            var br = new WebBrowser();
            br.DocumentCompleted += wb_DocumentCompleted3;
            br.Navigate(v);
            Application.Run();
        }
        static HtmlElement ElementsByClass(HtmlDocument doc, string className)
        {
            foreach (HtmlElement e in doc.All)
                if (e.GetAttribute("className") == className)
                    return e;
            return null;
        }
        [HandleProcessCorruptedStateExceptions]
        [SecurityCritical]
        private void wb_DocumentCompleted3(object sender, WebBrowserDocumentCompletedEventArgs e)
        {
            WebBrowser wb = sender as WebBrowser;
            HtmlDocument hd = null;
            try
            {
                hd = wb.Document;
            }
            catch (Exception e1)
            {
                Form1._resourcePool.Release();
                new DownloadTask(v, i, num, name, loc, html_chk);
                wb.Dispose();
                Application.ExitThread();
                return;
            }
            if (hd.Title.Equals("네이버 만화") || hd.Url.Equals("http://comic.naver.com/main.nhn"))
            {
                wb.Dispose();
                Form1._resourcePool.Release();
                Application.ExitThread();
                return;
            }
            HtmlElement he = ElementsByClass(hd, "wt_viewer");
            HtmlElementCollection trs = he.GetElementsByTagName("img");
            if (e.Url.AbsoluteUri != wb.Url.AbsoluteUri)
            {
                return;
            }
            string html = "<html><head><title> - <" + name + " " + i + "화" + "> - </title></head><body><center>";
            for (int j = 0; j < trs.Count; j++)
            {
                using (WebClient wc = new WebClient())
                {
                    if (!System.IO.Directory.Exists(loc + "\\" + name + "\\" + i + "화"))
                        System.IO.Directory.CreateDirectory(loc + "\\" + name + "\\" + i);
                    wc.Headers.Add("user-agent", "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.2; .NET CLR 1.0.3705;)");
                    wc.Headers.Add("Referer", "http://comic.naver.com/webtoon/detail.nhn?titleId=" + num + "&no=" + i + "화");
                    wc.DownloadFile(new System.Uri(trs[j].GetAttribute("src")), loc + "\\" + name + "\\" + i + "\\" + j + ".jpg");
                }
                html += "<img src=\"" + i + "\\" + j + ".jpg\">" + "<br>";
            }
            html += "</center></body></html>";
            if (html_chk)
            {
                using (FileStream fs = File.Create(loc + "\\" + name + "\\" + i + "화.html"))
                {
                    // Add some text to file
                    Byte[] title = new UnicodeEncoding().GetBytes(html);
                    fs.Write(title, 0, title.Length);
                }
            }
            wb.Dispose();
            Form1._resourcePool.Release();
            Application.ExitThread();
        }
    }
}