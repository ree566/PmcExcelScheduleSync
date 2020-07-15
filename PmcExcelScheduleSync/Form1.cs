using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.Entity;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using ExcelDataReader;

namespace PmcExcelScheduleSync
{
    public partial class Form1 : Form
    {
        static public string AppPath;

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            //Get the next date data
            DateTime dt = DateTime.Today;

            //Read Assy(Non pre) schedule list
            this.readAndSaveAssySchedule("\\\\aclfile.advantech.corp\\Group1\\DF\\PMC\\生產日排程\\APS 5F 組裝排程.xlsx", dt, 1);
            this.readAndSaveAssySchedule("\\\\aclfile.advantech.corp\\Group1\\DF\\PMC\\生產日排程\\APS 6F 組裝排程.xlsx", dt, 2);

            //Read pkg schedule list
            this.readAndSavePkgSchedule("\\\\aclfile.advantech.corp\\Group1\\DF\\PMC\\生產日排程\\TWM3 5F APS製程排程.xlsx", dt, 1);
            this.readAndSavePkgSchedule("\\\\aclfile.advantech.corp\\Group1\\DF\\PMC\\生產日排程\\TWM3 6F APS製程排程.xlsx", dt, 2);
            Application.Exit();
        }

        private DataSet readFile(string filePath)
        {
            //string file = Path.Combine(AppPath, filePath);
            string file = filePath;
            if (File.Exists(file))
            {
                var extension = Path.GetExtension(file).ToLower();
                Console.WriteLine("讀取檔案：" + file);
                using (var stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    //判斷格式套用讀取方法
                    IExcelDataReader reader = null;
                    if (extension == ".xls")
                    {
                        Console.WriteLine(" => XLS格式");
                        reader = ExcelReaderFactory.CreateBinaryReader(stream, new ExcelReaderConfiguration()
                        {
                            Password = "234",
                            FallbackEncoding = Encoding.GetEncoding("utf-8")
                        });
                    }
                    else if (extension == ".xlsx")
                    {
                        Console.WriteLine(" => XLSX格式");
                        reader = ExcelReaderFactory.CreateOpenXmlReader(stream, new ExcelReaderConfiguration()
                        {
                            Password = "234",
                            FallbackEncoding = Encoding.GetEncoding("utf-8")
                        });
                    }
                    else if (extension == ".csv")
                    {
                        Console.WriteLine(" => CSV格式");
                        reader = ExcelReaderFactory.CreateCsvReader(stream, new ExcelReaderConfiguration()
                        {
                            FallbackEncoding = Encoding.GetEncoding("utf-8")
                        });
                    }
                    else if (extension == ".txt")
                    {
                        Console.WriteLine(" => Text(Tab Separated)格式");
                        reader = ExcelReaderFactory.CreateCsvReader(stream, new ExcelReaderConfiguration()
                        {
                            FallbackEncoding = Encoding.GetEncoding("utf-8"),
                            AutodetectSeparators = new char[] { '\t' }
                        });
                    }

                    //沒有對應產生任何格式
                    if (reader == null)
                    {
                        //Console.WriteLine("未知的處理檔案：" + extension);
                        //Console.ReadKey();
                        //return 20;
                        return null;
                    }
                    Console.WriteLine(" => 轉換中");
                    using (reader)
                    {
                        DataSet ds = reader.AsDataSet(new ExcelDataSetConfiguration()
                        {
                            UseColumnDataType = false,
                            ConfigureDataTable = (tableReader) => new ExcelDataTableConfiguration()
                            {
                                //設定讀取資料時是否忽略標題
                                UseHeaderRow = false
                            }
                        });
                        return ds;
                    }
                }
                //Console.WriteLine("結束...按任意鍵離開...");
                //Console.ReadKey();
                //return 0;
            }
            else
            {
                Console.WriteLine("檔案 " + file + " 不存在!");
                //Console.ReadKey();
                return null;
                //return 18;
            }

        }

        private void readAndSaveAssySchedule(String filePath, DateTime dt, int floorId)
        {
            DataSet ds = this.readFile(filePath);
            if (ds == null)
            {
                return;
            }
            //Get 組裝 sheet
            var table = ds.Tables[0];

            //Get date field match current date
            int dateIndex = 0;
            for (var col = 1; col < table.Columns.Count; col++)
            {
                var check_data = table.Rows[5][col];
                if (dt.Equals(check_data))
                {
                    dateIndex = col;
                    break;
                }
            }

            int cnt = 0;

            ATMCEntities db = new ATMCEntities();

            //把 DataSet 顯示出來
            for (int row = 0; row < table.Rows.Count; row++)
            {
                if (row < 6)
                {
                    continue;
                }
                string quantityField = table.Rows[row][dateIndex].ToString();

                int i = 0;
                if (int.TryParse(quantityField, out i) == true && Int32.Parse(quantityField) > 0)
                {
                    var scheduleProcessField = table.Rows[row][1].ToString();
                    var modelField = table.Rows[row][2];

                    if (!System.DBNull.Value.Equals(modelField) && scheduleProcessField.Contains("BASSY"))
                    {
                        Console.WriteLine("Type: {0} and ModelName: {1}", modelField.GetType(), modelField);
                        //Console.WriteLine("TimeCost: {0}", table.Rows[row][dateIndex + 1]);

                        int totalQty = Int32.Parse(table.Rows[row][4].ToString());
                        int scheduleQty = Int32.Parse(table.Rows[row][dateIndex].ToString());
                        decimal timeCost = Decimal.Parse(table.Rows[row][dateIndex + 1].ToString());

                        db.PrepareSchedule.Add(new PrepareSchedule()
                        {
                            po = table.Rows[row][3].ToString().Trim(),
                            modelName = table.Rows[row][2].ToString().Trim(),
                            lineType_id = 1,
                            totalQty = totalQty,
                            scheduleQty = scheduleQty,
                            timeCost = timeCost,
                            floor_id = floorId,
                            onboardDate = dt,
                            priority = 0,
                            undoneQty = 0,
                            createDate = DateTime.Today
                        });

                        cnt++;
                    }
                }

            }
            db.SaveChanges();
            Console.WriteLine("Data total cnt: {0}", cnt);

        }

        private void readAndSavePkgSchedule(String filePath, DateTime dt, int floorId)
        {
            DataSet ds = this.readFile(filePath);
            if (ds == null)
            {
                return;
            }

            //Get 包裝 sheet
            var table = ds.Tables[2];

            //Get date field match current date
            int dateIndex = 0;
            for (var col = 1; col < table.Columns.Count; col++)
            {
                var check_data = table.Rows[5][col];
                if (dt.Equals(check_data))
                {
                    dateIndex = col;
                    break;
                }
            }

            int cnt = 0;

            ATMCEntities db = new ATMCEntities();

            //Get worktime data(移除沒有packingLeadTime的機種)
            List<vTb_WorkTime> worktimes = db.vTb_WorkTime.Where(w => w.packingLeadTime != 0).ToList();

            //把 DataSet 顯示出來
            for (int row = 0; row < table.Rows.Count; row++)
            {
                if (row < 6)
                {
                    continue;
                }
                string quantityField = table.Rows[row][dateIndex].ToString();
                var modelField = table.Rows[row][2];

                int i = 0;
                if (int.TryParse(quantityField, out i) == true && Int32.Parse(quantityField) > 0 && !System.DBNull.Value.Equals(modelField))
                {
                    {

                        Console.WriteLine("Type: {0} and ModelName: {1}", modelField.GetType(), modelField);
                        //Console.WriteLine("TimeCost: {0}", table.Rows[row][dateIndex + 1]);

                        string modelName = table.Rows[row][2].ToString().Trim();

                        var fitData = worktimes.FirstOrDefault(w => modelName.Equals(w.modelName));
                        if (fitData == null)
                        {
                            Console.WriteLine("{0}, packingLeadTime is zero", modelName);
                            continue;
                        }

                        int totalQty = Int32.Parse(table.Rows[row][4].ToString());
                        int scheduleQty = Int32.Parse(table.Rows[row][dateIndex].ToString());
                        //decimal timeCost = Decimal.Parse(table.Rows[row][dateIndex + 1].ToString());

                        //Console.WriteLine("{0}: {1} x {2} = {3}", modelName, 
                        //    fitData.packingLeadTime ?? 0, scheduleQty, (fitData.packingLeadTime ?? 0) * scheduleQty); 

                        //https://stackoverflow.com/questions/4871994/how-can-i-convert-decimal-to-decimal
                        db.PrepareSchedule.Add(new PrepareSchedule()
                        {
                            po = table.Rows[row][3].ToString().Trim(),
                            modelName = modelName,
                            lineType_id = 3,
                            totalQty = totalQty,
                            scheduleQty = scheduleQty,
                            timeCost = (fitData.packingLeadTime ?? 0) * scheduleQty,
                            floor_id = floorId,
                            onboardDate = dt,
                            priority = 0,
                            undoneQty = 0,
                            createDate = DateTime.Today
                        });

                        cnt++;
                    }
                }
            }
            db.SaveChanges();
            Console.WriteLine("Data total cnt: {0}", cnt);

        }
    }
}
