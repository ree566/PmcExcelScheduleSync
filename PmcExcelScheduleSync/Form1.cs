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

        private ATMCEntities db = new ATMCEntities();

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            List<DateTime> dts = new List<DateTime>();

            //Get the next date data
            //DateTime startDate = new DateTime(2020, 11, 2);
            DateTime startDate = DateTime.Today;

            int captureDays = 5;

            if (startDate.Hour >= 17)
            {
                startDate = startDate.AddDays(startDate.DayOfWeek == DayOfWeek.Saturday ? 2 : 1);
            }

            dts.Add(startDate);
            DateTime dt = startDate.AddDays(1);

            for (int i = 1; i <= captureDays; i++)
            {
                if (dt.DayOfWeek == DayOfWeek.Sunday)
                {
                    dt = dt.AddDays(1);
                }
                dts.Add(dt);
                dt = dt.AddDays(1);
            }

            //Read Assy(Non pre) schedule list
            this.readAndSaveAssySchedule("\\\\aclfile2.advantech.corp\\Group1\\DF\\PMC\\生產日排程\\APS 5F 組裝排程.xlsx", dts, 2);
            //this.readAndSaveAssySchedule("C:\\Users\\MFG.ESOP\\Desktop\\testExcel\\APS 5F 組裝排程1730.xlsx", dts, 2);

            //Read test schedule list
            this.readAndSaveTestSchedule("\\\\aclfile2.advantech.corp\\Group1\\DF\\PMC\\生產日排程\\TWM3 5F APS製程排程.xlsx", dts, 1);

            //Read pkg schedule list
            this.readAndSavePkgSchedule("\\\\aclfile2.advantech.corp\\Group1\\DF\\PMC\\生產日排程\\TWM3 5F APS製程排程.xlsx", dts, 1);

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
            }
            else
            {
                Console.WriteLine("檔案 " + file + " 不存在!");
                return null;
            }

        }

        private void readAndSaveAssySchedule(String filePath, List<DateTime> dts, int floorId)
        {

            int assyLineTypeId = 1;
            int preAssyLineTypeId = 9;
            int i = 0;//For testing table.row["N"] is a number or not 

            DataSet ds = this.readFile(filePath);
            if (ds == null)
            {
                return;
            }
            //Get 組裝 sheet
            var table = ds.Tables["5F--前置&組裝"];

            List<PrepareSchedule> dataInExcel = new List<PrepareSchedule>();

            foreach (DateTime dt in dts)
            {
                Console.WriteLine("Process ASSY {0} Data", dt.ToString("yyyy/MM/dd"));

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

                //把 DataSet 顯示出來
                for (int row = 0; row < table.Rows.Count; row++)
                {
                    if (row < 6)
                    {
                        continue;
                    }

                    string totalQtyField = table.Rows[row][4].ToString();
                    string scheduleQtyField = table.Rows[row][dateIndex].ToString();

                    if (int.TryParse(totalQtyField, out i) == true && Int32.Parse(totalQtyField) > 0 && int.TryParse(scheduleQtyField, out i) == true && Int32.Parse(scheduleQtyField) > 0)
                    {
                        var scheduleProcessField = table.Rows[row][1].ToString();
                        var modelField = table.Rows[row][2];

                        if (!System.DBNull.Value.Equals(modelField))
                        {
                            Console.WriteLine("Type: {0} and ModelName: {1}", modelField.GetType(), modelField);
                            //Console.WriteLine("TimeCost: {0}", table.Rows[row][dateIndex + 1]);
                            int totalQty = Int32.Parse(totalQtyField);
                            int scheduleQty = Int32.Parse(scheduleQtyField);
                            decimal timeCost = Decimal.Parse(table.Rows[row][dateIndex + 1].ToString());

                            dataInExcel.Add(new PrepareSchedule()
                            {
                                po = table.Rows[row][3].ToString().Trim(),
                                modelName = table.Rows[row][2].ToString().Trim(),
                                lineType_id = scheduleProcessField.Contains("BASSY") ? assyLineTypeId : preAssyLineTypeId,
                                totalQty = totalQty,
                                scheduleQty = scheduleQty,
                                timeCost = timeCost,
                                floor_id = floorId,
                                onboardDate = dt,
                                priority = 0,
                                undoneQty = 0,
                                createDate = DateTime.Today
                            });

                        }
                    }

                }
            }

            List<PrepareSchedule> dataInDb = db.PrepareSchedule.Where(
                p => dts.Contains(p.onboardDate) &&
                (p.lineType_id == preAssyLineTypeId || p.lineType_id == assyLineTypeId) &&
                p.floor_id == floorId)
                .ToList();

            //Compare data and add/remove
            var newData = dataInExcel.Except(dataInDb);
            var deletedData = dataInDb.Except(dataInExcel);

            db.PrepareSchedule.AddRange(newData);
            db.PrepareSchedule.RemoveRange(deletedData);
            db.SaveChanges();
            Console.WriteLine("FloorId {0} ASSY, Data in sql: {1}, data in excel: {2}",
                    floorId, dataInDb.Count(), dataInExcel.Count()
                );

            Console.WriteLine("FloorId {0} ASSY, Data total add cnt: {1}, remove cnt: {2}",
                    floorId, newData.Count(), deletedData.Count()
                );

            //var query = db.PrepareSchedule.GroupBy(x => x)
            //  .Where(g => g.Count() > 1)
            //  .Select(y => y.Key)
            //  .ToList();

        }

        private void readAndSaveTestSchedule(String filePath, List<DateTime> dts, int floorId)
        {
            //testLineTypeIds must equals sheetNames
            int[] testLineTypeIds = { 7, 8 };
            string[] sheetNames = { "5F--T1", "5F--T2" };

            int i = 0;//For testing table.row["N"] is a number or not 

            DataSet ds = this.readFile(filePath);
            if (ds == null)
            {
                return;
            }

            for (int k = 0; k < testLineTypeIds.Length; k++)
            {
                int testLineTypeId = testLineTypeIds[k];

                //Get T2 sheet
                var table = ds.Tables[sheetNames[k]];

                List<PrepareSchedule> dataInExcel = new List<PrepareSchedule>();

                foreach (DateTime dt in dts)
                {
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

                    //把 DataSet 顯示出來
                    for (int row = 0; row < table.Rows.Count; row++)
                    {
                        if (row < 6)
                        {
                            continue;
                        }

                        string totalQtyField = table.Rows[row][4].ToString();
                        string scheduleQtyField = table.Rows[row][dateIndex].ToString();

                        if (int.TryParse(totalQtyField, out i) == true && Int32.Parse(totalQtyField) > 0 && int.TryParse(scheduleQtyField, out i) == true && Int32.Parse(scheduleQtyField) > 0)
                        {
                            var modelField = table.Rows[row][2];

                            if (!System.DBNull.Value.Equals(modelField))
                            {
                                Console.WriteLine("Type: {0} and ModelName: {1}", modelField.GetType(), modelField);
                                //Console.WriteLine("TimeCost: {0}", table.Rows[row][dateIndex + 1]);
                                int totalQty = Int32.Parse(totalQtyField);
                                int scheduleQty = Int32.Parse(scheduleQtyField);
                                decimal timeCost = Decimal.Parse(table.Rows[row][dateIndex + 1].ToString());

                                dataInExcel.Add(new PrepareSchedule()
                                {
                                    po = table.Rows[row][3].ToString().Trim(),
                                    modelName = table.Rows[row][2].ToString().Trim(),
                                    lineType_id = testLineTypeId,
                                    totalQty = totalQty,
                                    scheduleQty = scheduleQty,
                                    timeCost = timeCost,
                                    floor_id = floorId,
                                    onboardDate = dt,
                                    priority = 0,
                                    undoneQty = 0,
                                    createDate = DateTime.Today
                                });

                            }
                        }

                    }
                }

                List<PrepareSchedule> dataInDb = db.PrepareSchedule.Where(
                    p => dts.Contains(p.onboardDate) &&
                    p.lineType_id == testLineTypeId &&
                    p.floor_id == floorId)
                    .ToList();

                //Compare data and add/remove
                var newData = dataInExcel.Except(dataInDb);
                var deletedData = dataInDb.Except(dataInExcel);

                db.PrepareSchedule.AddRange(newData);
                db.PrepareSchedule.RemoveRange(deletedData);
                db.SaveChanges();
                Console.WriteLine("FloorId {0} Test, Data in sql: {1}, data in excel: {2}",
                        floorId, dataInDb.Count(), dataInExcel.Count()
                    );

                Console.WriteLine("FloorId {0} Test, Data total add cnt: {1}, remove cnt: {2}",
                        floorId, newData.Count(), deletedData.Count()
                    );
            }
        }

        private void readAndSavePkgSchedule(String filePath, List<DateTime> dts, int floorId)
        {
            DataSet ds = this.readFile(filePath);
            if (ds == null)
            {
                return;
            }

            //Get 包裝 sheet
            var table = ds.Tables["5F--包裝"];

            int pkgLineTypeId = 3;
            List<PrepareSchedule> dataInExcel = new List<PrepareSchedule>();

            int i = 0;//For testing table.row["N"] is a number or not 

            foreach (DateTime dt in dts)
            {
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

                //Get worktime data(移除沒有packingLeadTime的機種)
                //List<vTb_WorkTime> worktimes = db.vTb_WorkTime.Where(w => w.packingLeadTime != 0).ToList();
                List<vTb_WorkTime> worktimes = db.vTb_WorkTime.ToList();

                //把 DataSet 顯示出來
                for (int row = 0; row < table.Rows.Count; row++)
                {
                    if (row < 6)
                    {
                        continue;
                    }

                    string totalQtyField = table.Rows[row][4].ToString();
                    string scheduleQtyField = table.Rows[row][dateIndex].ToString();
                    var modelField = table.Rows[row][2];

                    if (int.TryParse(totalQtyField, out i) == true && Int32.Parse(totalQtyField) > 0 && int.TryParse(scheduleQtyField, out i) == true && Int32.Parse(scheduleQtyField) > 0 && !System.DBNull.Value.Equals(modelField))
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

                            int totalQty = Int32.Parse(totalQtyField);
                            int scheduleQty = Int32.Parse(scheduleQtyField);
                            //decimal timeCost = Decimal.Parse(table.Rows[row][dateIndex + 1].ToString());

                            //Console.WriteLine("{0}: {1} x {2} = {3}", modelName, 
                            //    fitData.packingLeadTime ?? 0, scheduleQty, (fitData.packingLeadTime ?? 0) * scheduleQty); 

                            //https://stackoverflow.com/questions/4871994/how-can-i-convert-decimal-to-decimal
                            dataInExcel.Add(new PrepareSchedule()
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
                        }
                    }
                }
            }

            List<PrepareSchedule> dataInDb = db.PrepareSchedule.Where(
                p => dts.Contains(p.onboardDate) &&
                p.lineType_id == pkgLineTypeId &&
                p.floor_id == floorId)
                .ToList();

            //Compare data and add/remove
            var newData = dataInExcel.Except(dataInDb);
            var deletedData = dataInDb.Except(dataInExcel);

            db.PrepareSchedule.AddRange(newData);
            db.PrepareSchedule.RemoveRange(deletedData);
            db.SaveChanges();
            Console.WriteLine("FloorId {0} PKG,  Data in sql: {1}, data in excel: {2}",
                    floorId, dataInDb.Count(), dataInExcel.Count()
                );

            Console.WriteLine("FloorId {0} PKG, Data total add cnt: {1}, remove cnt: {2}",
                    floorId, newData.Count(), deletedData.Count()
                );

        }
    }
}
