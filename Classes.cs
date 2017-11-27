﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Data;
using System.Xml.Serialization;
using Microsoft.VisualBasic;
using NPOI.HSSF.UserModel;
using NPOI.HPSF;
using NPOI.SS.UserModel;
using Microsoft.VisualBasic.FileIO;

namespace TournamentGenerator
{
    public static class ConfigValues
    {
        public static int fightGenerationRetryLimit;
        public static List<string> poolNames;
        public static List<int> eliminationSizes;
    }

    public static class Helpers
    {
        public static Random rng = new Random();

        /// <summary>
        /// Randomise the order of items in a generic list
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list">The list to shuffle</param>
        public static void Shuffle<T>(this IList<T> list)
        {
            int size = list.Count;
            while (size > 1)
            {
                size--;
                int index = rng.Next(size + 1);
                T value = list[index];
                list[index] = list[size];
                list[size] = value;
            }
        }

        /// <summary>
        /// Return a list of every possible distinct value pairing from a given list
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <returns></returns>
        public static List<T[]> GetDistinctPairs<T>(this IList<T> list)
        {
            List<T[]> pairs = new List<T[]>();

            for (int i = 0; i < list.Count - 1; i++)
            {
                for (int j = (i + 1); j < list.Count; j++)
                {
                    pairs.Add(new T[] { list[i], list[j] });
                }
            }

            return pairs;
        }
    }

    public class Country
    {
        public static List<Country> Countries = new List<Country>();

        public string name;
        public string code;

        public static void LoadCountries(string csvFile)
        {
            Countries.Clear();

            using (TextFieldParser parser = new TextFieldParser(csvFile))
            {
                parser.TextFieldType = FieldType.Delimited;
                parser.SetDelimiters(",");
                while (!parser.EndOfData)
                {
                    string[] fields = parser.ReadFields();

                    Country c = new Country();
                    c.name = fields[0];
                    c.code = fields[1];

                    Countries.Add(c);
                }
            }
        }

        public override string ToString()
        {
            return name;
        }
    }

    public static class FileAccessHelper
    {
        public static void SaveTournament(Tournament tournament, string filePath)
        {
            XmlSerializer xml = new XmlSerializer(typeof(Tournament));

            using (TextWriter w = new StreamWriter(filePath))
            {
                xml.Serialize(w, tournament);
                w.Close();
            }
        }

        public static Tournament LoadTournament(string filePath)
        {
            Tournament tournament = null;

            XmlSerializer xml = new XmlSerializer(typeof(Tournament));

            using (Stream s = new FileStream(filePath, FileMode.Open))
            {
                tournament = (Tournament)xml.Deserialize(s);

                s.Close();
            }

            return tournament;
        }

        public static void GenerateSpreadsheet(Tournament tournament, string filePath)
        {
            List<Pool> pools = tournament.pools;

            HSSFWorkbook book = new HSSFWorkbook();

            //create a entry of DocumentSummaryInformation
            DocumentSummaryInformation dsi = PropertySetFactory.CreateDocumentSummaryInformation();
            dsi.Company = "SwordJet";
            book.DocumentSummaryInformation = dsi;

            //create a entry of SummaryInformation
            SummaryInformation si = PropertySetFactory.CreateSummaryInformation();
            si.Subject = "Generated Tournament";
            book.SummaryInformation = si;

            IFont plainFont = book.CreateFont();
            plainFont.Boldweight = (short)FontBoldWeight.Normal;

            //create the basic left-aligned style
            ICellStyle plainStyle = book.CreateCellStyle();
            plainStyle.SetFont(plainFont);
            plainStyle.WrapText = true;
            plainStyle.VerticalAlignment = VerticalAlignment.Top;
            plainStyle.Alignment = NPOI.SS.UserModel.HorizontalAlignment.Left;

            //create the basic center-aligned style
            ICellStyle plainCenterStyle = book.CreateCellStyle();
            plainCenterStyle.SetFont(plainFont);
            plainCenterStyle.WrapText = true;
            plainCenterStyle.VerticalAlignment = VerticalAlignment.Top;
            plainCenterStyle.Alignment = NPOI.SS.UserModel.HorizontalAlignment.Center;

            IFont boldFont = book.CreateFont();
            boldFont.Boldweight = (short)FontBoldWeight.Bold;

            //create the header style
            ICellStyle headerStyle = book.CreateCellStyle();
            headerStyle.SetFont(boldFont);
            headerStyle.WrapText = false;
            headerStyle.VerticalAlignment = VerticalAlignment.Center;
            headerStyle.Alignment = NPOI.SS.UserModel.HorizontalAlignment.Center;

            //add each pool to a seperate sheet
            foreach (Pool pool in pools)
            {
                int rowIndex = 0;

                //create new sheet for this pool
                ISheet sheet = book.CreateSheet("Pool - " + pool.name);
                sheet.PrintSetup.Landscape = true;
                sheet.PrintSetup.FitHeight = 0;

                //create a merged title cell with the pool name
                IRow topRow = sheet.CreateRow(rowIndex);
                ICell topCell = topRow.CreateCell(topRow.Cells.Count);
                topCell.SetCellValue("POOL - " + pool.name);
                topCell.CellStyle = headerStyle;
                NPOI.SS.Util.CellRangeAddress cra = new NPOI.SS.Util.CellRangeAddress(rowIndex, rowIndex, 0, 1);
                sheet.AddMergedRegion(cra);

                rowIndex += 2;

                //create the fighters header row
                IRow headerRow = sheet.CreateRow(rowIndex);

                ICell headerCellID = headerRow.CreateCell(headerRow.Cells.Count);
                headerCellID.SetCellValue("ID");
                headerCellID.CellStyle = headerStyle;

                ICell headerCellName = headerRow.CreateCell(headerRow.Cells.Count);
                headerCellName.SetCellValue("Name");
                headerCellName.CellStyle = headerStyle;

                ICell headerCellScore = headerRow.CreateCell(headerRow.Cells.Count);
                headerCellScore.SetCellValue("Score");
                headerCellScore.CellStyle = headerStyle;

                ICell headerCellDoubles = headerRow.CreateCell(headerRow.Cells.Count);
                headerCellDoubles.SetCellValue("Doubles");
                headerCellDoubles.CellStyle = headerStyle;

                rowIndex += 2;

                //add each fighter in the pool to the pool sheet
                foreach (int fighterId in pool.fighters)
                {
                    Fighter fighter = tournament.fighters.Where(f => f.id == fighterId).First();
                    IRow row = sheet.CreateRow(rowIndex);

                    ICell cellID = row.CreateCell(row.Cells.Count);
                    cellID.SetCellValue(fighter.id);
                    cellID.CellStyle = plainStyle;

                    ICell cellName = row.CreateCell(row.Cells.Count);
                    cellName.SetCellValue(fighter.name);
                    cellName.CellStyle = plainStyle;

                    ICell cellScore = row.CreateCell(row.Cells.Count);
                    cellScore.CellStyle = plainStyle;
                    fighter.scoreCellRef = new int[] { rowIndex, 2 };

                    ICell cellDoubles = row.CreateCell(row.Cells.Count);
                    cellDoubles.CellStyle = plainStyle;
                    fighter.doubleCellRef = new int[] { rowIndex, 3 };

                    fighter.scoreFormula = "";
                    fighter.doubleFormula = "";

                    rowIndex++;
                }

                rowIndex += 3;

                //add each round of fights to the pool sheet
                foreach (List<Fight> round in pool.rounds)
                {
                    //add merged title cell
                    IRow roundTitleRow = sheet.CreateRow(rowIndex);
                    ICell roundTitleCell = roundTitleRow.CreateCell(roundTitleRow.Cells.Count);
                    roundTitleCell.SetCellValue("ROUND - " + (pool.rounds.IndexOf(round) + 1));
                    roundTitleCell.CellStyle = headerStyle;
                    NPOI.SS.Util.CellRangeAddress cra2 = new NPOI.SS.Util.CellRangeAddress(rowIndex, rowIndex, 0, 6);
                    sheet.AddMergedRegion(cra2);

                    rowIndex++;

                    //create the round headers row
                    IRow headerRowFights = sheet.CreateRow(rowIndex);

                    ICell headerCellDoublesFigherA = headerRowFights.CreateCell(headerRowFights.Cells.Count);
                    headerCellDoublesFigherA.SetCellValue("Doubles");
                    headerCellDoublesFigherA.CellStyle = headerStyle;

                    ICell headerCellScoreFighterA = headerRowFights.CreateCell(headerRowFights.Cells.Count);
                    headerCellScoreFighterA.SetCellValue("Score");
                    headerCellScoreFighterA.CellStyle = headerStyle;

                    ICell headerCellNameFighterA = headerRowFights.CreateCell(headerRowFights.Cells.Count);
                    headerCellNameFighterA.SetCellValue("Name");
                    headerCellNameFighterA.CellStyle = headerStyle;

                    ICell headerCellV = headerRowFights.CreateCell(headerRowFights.Cells.Count);
                    headerCellV.SetCellValue("v");
                    headerCellV.CellStyle = headerStyle;

                    ICell headerCellNameFighterB = headerRowFights.CreateCell(headerRowFights.Cells.Count);
                    headerCellNameFighterB.SetCellValue("Name");
                    headerCellNameFighterB.CellStyle = headerStyle;

                    ICell headerCellScoreFighterB = headerRowFights.CreateCell(headerRowFights.Cells.Count);
                    headerCellScoreFighterB.SetCellValue("Score");
                    headerCellScoreFighterB.CellStyle = headerStyle;

                    ICell headerCellDoublesFigherB = headerRowFights.CreateCell(headerRowFights.Cells.Count);
                    headerCellDoublesFigherB.SetCellValue("Doubles");
                    headerCellDoublesFigherB.CellStyle = headerStyle;

                    rowIndex += 2;

                    //add each fight to the round
                    foreach (Fight fight in round)
                    {
                        IRow row = sheet.CreateRow(rowIndex);

                        Fighter fighterA = tournament.fighters.Where(f => f.id == fight.fighterA).First();
                        Fighter fighterB = tournament.fighters.Where(f => f.id == fight.fighterB).First();

                        ICell cellNameFighterADoubles = row.CreateCell(row.Cells.Count);
                        cellNameFighterADoubles.CellStyle = plainStyle;
                        //update the fighter's doubles formula
                        if (fighterA.doubleFormula != "") fighterA.doubleFormula += ("+A" + (rowIndex + 1));
                        else fighterA.doubleFormula += ("A" + (rowIndex + 1));

                        ICell cellNameFighterAScore = row.CreateCell(row.Cells.Count);
                        cellNameFighterAScore.CellStyle = plainStyle;
                        //update the fighter's score formula
                        if (fighterA.scoreFormula != "") fighterA.scoreFormula += ("+B" + (rowIndex + 1));
                        else fighterA.scoreFormula += ("B" + (rowIndex + 1));

                        ICell cellNameFighterA = row.CreateCell(row.Cells.Count);
                        cellNameFighterA.SetCellValue(fighterA.name);
                        cellNameFighterA.CellStyle = plainStyle;

                        ICell cellV = row.CreateCell(row.Cells.Count);
                        cellV.SetCellValue("v");
                        cellV.CellStyle = plainCenterStyle;

                        ICell cellNameFighterB = row.CreateCell(row.Cells.Count);
                        cellNameFighterB.SetCellValue(fighterB.name);
                        cellNameFighterB.CellStyle = plainStyle;

                        ICell cellNameFighterBScore = row.CreateCell(row.Cells.Count);
                        cellNameFighterBScore.CellStyle = plainStyle;
                        if (fight.oddFight)
                        {
                            cellNameFighterBScore.SetCellValue("X");
                        }
                        else
                        {
                            //update the fighter's score formula
                            if (fighterB.scoreFormula != "") fighterB.scoreFormula += ("+F" + (rowIndex + 1));
                            else fighterB.scoreFormula += ("F" + (rowIndex + 1));
                        }

                        ICell cellNameFighterBDoubles = row.CreateCell(row.Cells.Count);
                        cellNameFighterBDoubles.CellStyle = plainStyle;
                        if (fight.oddFight)
                        {
                            cellNameFighterBDoubles.SetCellValue("X");
                        }
                        else
                        {
                            //update the fighter's doubles formula
                            if (fighterB.doubleFormula != "") fighterB.doubleFormula += ("+G" + (rowIndex + 1));
                            else fighterB.doubleFormula += ("G" + (rowIndex + 1));
                        }

                        rowIndex++;
                    }

                    rowIndex += 3;
                }

                //update each pool fighter score and doubles forumlae
                foreach (int fighterId in pool.fighters)
                {
                    Fighter fighter = tournament.fighters.Where(f => f.id == fighterId).First();

                    if (fighter.scoreCellRef != null)
                    {
                        IRow scoreRow = sheet.GetRow(fighter.scoreCellRef[0]);
                        ICell scoreCell = scoreRow.GetCell(fighter.scoreCellRef[1]);
                        scoreCell.SetCellFormula(fighter.scoreFormula);
                    }

                    if (fighter.doubleCellRef != null)
                    {
                        IRow doubleRow = sheet.GetRow(fighter.doubleCellRef[0]);
                        ICell doubleCell = doubleRow.GetCell(fighter.doubleCellRef[1]);
                        doubleCell.SetCellFormula(fighter.doubleFormula);
                    }

                    //reset the fighter excel variables
                    fighter.scoreFormula = "";
                    fighter.doubleFormula = "";
                    fighter.doubleCellRef = null;
                    fighter.scoreCellRef = null;
                }

                FileStream stream = new FileStream(filePath, FileMode.Create);
                book.Write(stream);
                stream.Close();

                book.Close();
            }
        }
    }

    [Serializable]
    public class Tournament
    {
        public enum TournamentStage { REGISTRATION = 0, POOLFIGHTS, TIEBREAKERS, ELIMINATIONS, FINALS, CLOSED }
        public enum PoolType { FIXEDROUNDS = 0, ROUNDROBIN = 1, SWISSPAIRS = 2 }
        public enum EliminationType { RANDOMISED = 0, MATCHED = 1 }

        public string name;
        public int numberOfRounds;
        public int numberOfPools;
        public int fightTimeMinutes;
        public TournamentStage stage = TournamentStage.REGISTRATION;
        public PoolType poolType = PoolType.FIXEDROUNDS;
        public EliminationType eliminationType = EliminationType.RANDOMISED;
        public int eliminationSize;
        public bool matchedEliminations;
        public int winPoints;
        public int drawPoints;
        public int lossPoints;
        public int? doubleThreshold;

        public List<Fighter> fighters = new List<Fighter>();
        public List<Pool> pools = new List<Pool>();
        public Pool tieBreakers = null;
        public List<Pool> eliminations = new List<Pool>();
        public List<Fight> finals = new List<Fight>();

        public Tournament()
        {
            name = "Tournament - " + DateTime.Today.ToString("dd MM yyyy");
            numberOfPools = 1;
            numberOfRounds = 1;
            fightTimeMinutes = 1;
            eliminationSize = 8;
            matchedEliminations = false;
            winPoints = 3;
            drawPoints = 2;
            lossPoints = 1;
            doubleThreshold = null;
        }

        public int GetNextFighterID()
        {
            if (fighters.Count > 0)
            {
                return fighters.OrderBy(o => o.id).Last().id + 1;
            }
            return 1;
        }

        public Fighter GetFighterByID(int id)
        {
            foreach (Fighter f in fighters)
            {
                if (f.id == id) return f;
            }

            return null;
        }

        public Fight GetFightByID(Guid id)
        {
            foreach (Pool p in pools)
            {
                foreach (List<Fight> r in p.rounds)
                {
                    foreach (Fight f in r)
                    {
                        if (f.fightID == id) return f;
                    }
                }
            }

            foreach (Pool p in eliminations)
            {
                foreach (List<Fight> r in p.rounds)
                {
                    foreach (Fight f in r)
                    {
                        if (f.fightID == id) return f;
                    }
                }
            }

            if (tieBreakers != null)
            {
                foreach (List<Fight> r in tieBreakers.rounds)
                {
                    foreach (Fight f in r)
                    {
                        if (f.fightID == id) return f;
                    }
                }
            }

            foreach (Fight f in finals)
            {
                if (f.fightID == id) return f;
            }

            return null;
        }

        public List<Fight> GetPoolFightsByFighter(int fighterId)
        {
            List<Fight> fights = new List<Fight>();

            foreach(Pool p in pools)
            {
                foreach(List<Fight> round in p.rounds)
                {
                    foreach(Fight f in round)
                    {
                        if (f.fighterA == fighterId || f.fighterB == fighterId) fights.Add(f);
                    }
                }
            }

            return fights;
        }

        public bool HasFightHappenedAlready(Fight f)
        {
            return (FindFight(f) != null);
        }

        public Fight FindFight(Fight f)
        {
            foreach (Pool p in pools)
            {
                Fight fight = p.FindFight(f);
                if(fight != null) return fight;
            }

            return null;
        }

        public DataView GetFightersDataView()
        {
            DataTable table = new DataTable();

            if (stage == TournamentStage.CLOSED)
            {
                table.Columns.Add("FinishingRank", typeof(int));
            }

            table.Columns.Add("Name", typeof(string));
            table.Columns.Add("Pool", typeof(string));
            table.Columns.Add("PoolScore", typeof(int));
            table.Columns.Add("PoolDoubles", typeof(int));
            table.Columns.Add("PoolBuchholz", typeof(int));

            foreach (Pool p in eliminations)
            {
                table.Columns.Add(p.name, typeof(string));
            }

            if (finals.Count > 0)
            {
                table.Columns.Add("Finals", typeof(string));
            }

            table.Columns.Add("TieBreakerScore", typeof(int));
            table.Columns.Add("ElimSort", typeof(int));

            foreach (Fighter fighter in fighters)
            {
                DataRow row = table.NewRow();

                if (stage == Tournament.TournamentStage.CLOSED)
                {
                    int rank = GetFighterFinalRank(fighter);
                    row["FinishingRank"] = rank;
                }

                row["Name"] = fighter.name;

                string poolname = "";
                foreach (Pool p in pools)
                {
                    if (p.fighters.Contains(fighter.id)) poolname = p.name;
                }
                row["Pool"] = poolname;

                row["PoolScore"] = GetFighterScore(fighter);

                row["PoolDoubles"] = GetFighterDoubles(fighter);

                row["PoolBuchholz"] = GetFighterBuchholzScore(fighter);

                row["TieBreakerScore"] = GetFighterTieBreakerScore(fighter);

                Dictionary<string, string> elimResults = GetFighterEliminationResults(fighter);
                int elimSort = 0;

                for (int i = 0; i < eliminations.Count; i++)
                {
                    Pool p = eliminations[i];
                    string r = elimResults[p.name];
                    row[p.name] = r;
                    elimSort += ((r == "WIN") ? 2 * (i + 1) : ((r == "LOSS") ? 1 * (i + 1) : 0));
                }

                if (finals.Count > 0)
                {
                    string r = GetFighterFinalsResult(fighter);
                    row["Finals"] = r;
                    elimSort += ((r == "WIN") ? 2 * eliminations.Count : ((r == "LOSS") ? 1 * eliminations.Count : 0));
                }

                row["ElimSort"] = elimSort;
                table.Rows.Add(row);
            }

            DataView dv = table.DefaultView;
            dv.Sort = "ElimSort DESC, PoolScore DESC, PoolDoubles ASC, PoolBuchholz DESC, TieBreakerScore DESC";
            if (stage == TournamentStage.CLOSED)
            {
                dv.Sort = "FinishingRank ASC, " + dv.Sort;
            }

            return dv;
        }

        public int GetFighterScore(Fighter fighter)
        {
            int score = 0;

            foreach (Pool pool in pools)
            {
                if (pool.fighters.Contains(fighter.id))
                {
                    foreach (List<Fight> round in pool.rounds)
                    {
                        foreach (Fight fight in round)
                        {
                            Fight.FightResult result = Fight.FightResult.PENDING;

                            if (fight.fighterA == fighter.id)
                            {
                                result = fight.fighterAResult;
                            }
                            else if (fight.fighterB == fighter.id && !fight.oddFight)
                            {
                                result = fight.fighterBResult;
                            }

                            if (result != Fight.FightResult.PENDING)
                            {
                                int gainedScore = 0;

                                switch (result)
                                {
                                    case Fight.FightResult.WIN: gainedScore = winPoints; break;
                                    case Fight.FightResult.LOSS: gainedScore = lossPoints; break;
                                    case Fight.FightResult.DRAW: gainedScore = drawPoints; break;
                                    case Fight.FightResult.DQ: gainedScore = 0; break;
                                }

                                score += gainedScore;
                                break;
                            }
                        }
                    }
                }
            }

            return score;
        }

        public int GetFighterDoubles(Fighter fighter)
        {
            int doubles = 0;

            foreach (Pool pool in pools)
            {
                if (pool.fighters.Contains(fighter.id))
                {
                    foreach (List<Fight> round in pool.rounds)
                    {
                        foreach (Fight fight in round)
                        {
                            Fight.FightResult result = Fight.FightResult.PENDING;

                            if (fight.fighterA == fighter.id)
                            {
                                result = fight.fighterAResult;
                            }
                            else if (fight.fighterB == fighter.id && !fight.oddFight)
                            {
                                result = fight.fighterBResult;
                            }

                            if (result != Fight.FightResult.PENDING)
                            {
                                doubles += fight.doubleCount;
                                break;
                            }
                        }
                    }
                }
            }

            return doubles;
        }

        public int GetFighterBuchholzScore(Fighter fighter)
        {
            int buchholz = 0;

            foreach (Pool pool in pools)
            {
                if (pool.fighters.Contains(fighter.id))
                {
                    foreach (List<Fight> round in pool.rounds)
                    {
                        foreach (Fight fight in round)
                        {
                            if (!fight.oddFight)
                            {
                                Fight.FightResult result = Fight.FightResult.PENDING;

                                if (fight.fighterA == fighter.id)
                                {
                                    result = fight.fighterAResult;
                                }
                                else if (fight.fighterB == fighter.id && !fight.oddFight)
                                {
                                    result = fight.fighterBResult;
                                }

                                if (result != Fight.FightResult.PENDING)
                                {
                                    Fighter opponent = null;
                                    if (fight.fighterA == fighter.id) opponent = GetFighterByID(fight.fighterB);
                                    else opponent = GetFighterByID(fight.fighterA);

                                    int opponentScore = GetFighterScore(opponent);
                                    int opponentDoubles = GetFighterDoubles(opponent);

                                    buchholz += (opponentScore - opponentDoubles);

                                    break;
                                }
                            }
                        }
                    }
                }
            }

            return buchholz;
        }

        public int GetFighterTieBreakerScore(Fighter fighter)
        {
            if (tieBreakers != null)
            {
                if (tieBreakers.fighters.Contains(fighter.id))
                {
                    int score = 0;

                    foreach (List<Fight> r in tieBreakers.rounds)
                    {
                        foreach (Fight f in r)
                        {
                            if (f.fighterA == fighter.id) score += (f.fighterAResult == Fight.FightResult.WIN) ? 1 : 0;
                            else if (f.fighterB == fighter.id && !f.oddFight) score += (f.fighterBResult == Fight.FightResult.WIN) ? 1 : 0;
                        }
                    }

                    return score;
                }
            }

            return 0;
        }

        public Dictionary<string, string> GetFighterEliminationResults(Fighter fighter)
        {
            Dictionary<string, string> retList = new Dictionary<string, string>();

            foreach (Pool p in eliminations)
            {
                string result = "-";

                if (p.fighters.Contains(fighter.id))
                {
                    foreach (Fight f in p.rounds[0])
                    {
                        if (f.fighterA == fighter.id)
                        {
                            result = f.fighterAResult.ToString();
                            break;
                        }
                        if (f.fighterB == fighter.id)
                        {
                            result = f.fighterBResult.ToString();
                            break;
                        }
                    }
                }

                retList.Add(p.name, result);
            }

            return retList;
        }

        public string GetFighterFinalsResult(Fighter fighter)
        {
            foreach (Fight f in finals)
            {
                if (f.fighterA == fighter.id)
                {
                    return f.fighterAResult.ToString();
                }
                if (f.fighterB == fighter.id)
                {
                    return f.fighterBResult.ToString();
                }
            }

            return "-";
        }

        public int GetFighterFinalRank(Fighter fighter)
        {
            if (stage == TournamentStage.CLOSED)
            {
                if (finals[1].fighterA == fighter.id)
                {
                    if (finals[1].fighterAResult == Fight.FightResult.WIN) return 1;
                    else return 2;
                }
                else if (finals[1].fighterB == fighter.id)
                {
                    if (finals[1].fighterBResult == Fight.FightResult.WIN) return 1;
                    else return 2;
                }

                if (finals[0].fighterA == fighter.id)
                {
                    if (finals[0].fighterAResult == Fight.FightResult.WIN) return 3;
                    else return 4;
                }
                else if (finals[0].fighterB == fighter.id)
                {
                    if (finals[0].fighterBResult == Fight.FightResult.WIN) return 3;
                    else return 4;
                }

                for (int i = eliminations.Count - 1; i > -1; i--)
                {
                    int bracketRank = 4 + -(i - (eliminations.Count - 1));
                    foreach (Fight f in eliminations[i].rounds[0])
                    {
                        if (f.fighterA == fighter.id && f.fighterAResult == Fight.FightResult.LOSS) return bracketRank;
                        if (f.fighterB == fighter.id && f.fighterBResult == Fight.FightResult.LOSS) return bracketRank;
                    }
                }
            }

            return fighters.Count;
        }

        public bool IsLatestBracket(Pool p)
        {
            if (finals.Count > 0) return false;

            if (eliminations.Count > 0)
            {
                if (eliminations.Last().name == p.name) return true;
            }
            else
            {
                if (tieBreakers != null && tieBreakers.name == p.name) return true;
            }

            return false;
        }

        public bool ExtendPools()
        {
            if (stage == TournamentStage.POOLFIGHTS)
            {
                int poolFighters = pools[0].fighters.Count;

                //ensure pools can be extended
                if ((poolFighters - 1) <= (numberOfRounds + 1)) return false;

                foreach (Pool p in pools)
                {
                    p.GenerateRound();
                }

                numberOfRounds++;

                return true;
            }

            return false;
        }

        public Pool GeneratePool(string name, List<int> fighters)
        {
            Pool pool = new Pool();
            pool.name = name;
            pool.fighters = fighters;

            int roundsThisPool = numberOfRounds;
            if (poolType == PoolType.ROUNDROBIN) roundsThisPool = fighters.Count - 1;

            for (int k = 0; k < roundsThisPool; k++)
            {
                List<Fight> round = pool.GenerateRound();

                if (round == null) return null;
            }

            return pool;
        }

        public List<Pool> GeneratePools()
        {
            switch (poolType)
            {
                case PoolType.FIXEDROUNDS:
                    return GenerateFixedPools();

                case PoolType.ROUNDROBIN:
                    return GenerateFixedPools();

                case PoolType.SWISSPAIRS:
                    return GenerateSwissPools();

                default:
                    return null;
            }
        }

        public List<Pool> GenerateSwissPools()
        {
            DataTable fighterTable = new DataTable();
            fighterTable.Columns.Add("ID", typeof(int));
            fighterTable.Columns.Add("Score", typeof(int));
            fighterTable.Columns.Add("Doubles", typeof(int));
            fighterTable.Columns.Add("Random", typeof(int));

            foreach (Fighter f in fighters)
            {
                DataRow fRow = fighterTable.NewRow();

                fRow["ID"] = f.id;
                fRow["Score"] = GetFighterScore(f);
                fRow["Doubles"] = GetFighterDoubles(f);
                fRow["Random"] = Helpers.rng.Next(0, fighters.Count * 2);

                fighterTable.Rows.Add(fRow);
            }

            DataView vw = new DataView(fighterTable);
            vw.Sort = "Score DESC, Doubles ASC, Random ASC";

            int poolSwap = 0;

            List<Fight> topFights = null;
            List<Fight> bottomFights = null;

            while (topFights == null || bottomFights == null)
            {
                List<int> topFighters = new List<int>();
                List<int> bottomFighters = new List<int>();

                int firstPoolSize = vw.Count / 2;
                if (firstPoolSize % 2 == 1)
                {
                    if (pools.Count / 2 < numberOfRounds / 2)
                    {
                        firstPoolSize++;
                    }
                    else
                    {
                        firstPoolSize--;
                    }
                }

                for (int i = 0; i < vw.Count; i++)
                {
                    if (i > firstPoolSize)
                    {
                        bottomFighters.Add((int)vw[i]["ID"]);
                    }
                    else
                    {
                        topFighters.Add((int)vw[i]["ID"]);
                    }
                }

                for(int i = 0; i < poolSwap; i++)
                {
                    int topFighterSwap = topFighters[topFighters.Count - (1 + i)];
                    int bottomFighterSwap = bottomFighters[i];

                    topFighters.Remove(topFighterSwap);
                    topFighters.Add(bottomFighterSwap);
                    bottomFighters.Remove(bottomFighterSwap);
                    bottomFighters.Add(topFighterSwap);
                }

                Pool topPool = new Pool();
                topPool.fighters = topFighters;
                topPool.name = "Top Pool " + ((pools.Count / 2) + 1).ToString();
                topFights = topPool.GenerateSwissRound(this);

                Pool bottomPool = new Pool();
                bottomPool.fighters = bottomFighters;
                bottomPool.name = "Bottom Pool " + ((pools.Count / 2) + 1).ToString();
                bottomFights = bottomPool.GenerateSwissRound(this);

                if (topFights != null && bottomFights != null)
                {
                    pools.Add(topPool);
                    pools.Add(bottomPool);
                }
                else poolSwap++;
            }

            return pools;
        }

        public List<Pool> GenerateFixedPools()
        {
            int fightersPerPool = fighters.Count / numberOfPools;
            pools = new List<Pool>();

            //clone the list of fighters so we don't remove from the master list
            List<Fighter> fightersClone = new List<Fighter>();
            fightersClone.AddRange(fighters);

            for (int i = 0; i < numberOfPools; i++)
            {
                List<string> poolNames = new List<string>();
                poolNames.AddRange(ConfigValues.poolNames);

                int nameIndex = Helpers.rng.Next(0, poolNames.Count);
                string name = poolNames[nameIndex];
                poolNames.RemoveAt(nameIndex);

                List<int> poolFighters = new List<int>();

                //add random fighters to the pool until we have the correct size
                for (int j = 0; j < fightersPerPool; j++)
                {
                    int randIndex = Helpers.rng.Next(0, fightersClone.Count);
                    poolFighters.Add(fightersClone[randIndex].id);
                    fightersClone.RemoveAt(randIndex);
                }

                //if there are any odd fighters, add them to the last pool
                if (i == numberOfPools - 1 && fightersClone.Count > 0)
                {
                    while (fightersClone.Count > 0)
                    {
                        poolFighters.Add(fightersClone[0].id);
                        fightersClone.RemoveAt(0);
                    }
                }

                pools.Add(GeneratePool(name, poolFighters));
            }

            return pools;
        }

        public bool GenerateNextEliminationBracket()
        {
            Pool bracket = new Pool();

            if (eliminations.Count > 0)
            {
                foreach (Fight f in eliminations.Last().rounds.Last())
                {
                    if (f.fighterAResult == Fight.FightResult.WIN) bracket.fighters.Add(f.fighterA);
                    else bracket.fighters.Add(f.fighterB);
                }
            }
            else
            {
                DataTable table = new DataTable();
                table.Columns.Add("ID", typeof(int));
                table.Columns.Add("Score", typeof(int));
                table.Columns.Add("Doubles", typeof(int));
                table.Columns.Add("TieBreaker", typeof(int));
                table.Columns.Add("Buchholz", typeof(int));

                foreach (Fighter fighter in fighters)
                {
                    DataRow row = table.NewRow();

                    row["ID"] = fighter.id;
                    row["Score"] = GetFighterScore(fighter);
                    row["Doubles"] = GetFighterDoubles(fighter);
                    row["TieBreaker"] = GetFighterTieBreakerScore(fighter);
                    row["Buchholz"] = GetFighterBuchholzScore(fighter);

                    table.Rows.Add(row);
                }

                DataView dv = table.DefaultView;
                dv.Sort = "Score DESC, Doubles ASC, Buchholz DESC, TieBreaker DESC";

                for (int i = 0; i < eliminationSize; i++)
                {
                    if (i == eliminationSize - 1)
                    {
                        //handle tie-breakers
                        if (tieBreakers == null)
                        {
                            List<int> tiedFighters = new List<int>();
                            tiedFighters.Add((int)dv[i]["ID"]);

                            int j = i + 1;

                            int lastPlaceScore = (int)dv[i]["Score"];
                            int lastPlaceDoubles = (int)dv[i]["Doubles"];
                            int lastPlaceBuchholz = (int)dv[i]["Buchholz"];

                            //work down list
                            while (lastPlaceScore == (int)dv[j]["Score"] && lastPlaceDoubles == (int)dv[j]["Doubles"] && lastPlaceBuchholz == (int)dv[j]["Buchholz"])
                            {
                                tiedFighters.Add((int)dv[j]["ID"]);
                                j++;
                            }

                            j = i - 1;

                            if (tiedFighters.Count > 1)
                            {
                                //work up list
                                while (lastPlaceScore == (int)dv[j]["Score"] && lastPlaceDoubles == (int)dv[j]["Doubles"] && lastPlaceBuchholz == (int)dv[j]["Buchholz"])
                                {
                                    tiedFighters.Add((int)dv[j]["ID"]);
                                    j--;
                                }

                                tieBreakers = new Pool();
                                tieBreakers.name = "Tie Breakers";
                                tieBreakers.fighters = tiedFighters;

                                List<Fight> tieBreakerFights = new List<Fight>();

                                //ensure every permutation happens
                                List<int[]> pairs = tieBreakers.fighters.GetDistinctPairs();
                                foreach (int[] pair in pairs)
                                {
                                    Fight f = new Fight(pair[0], pair[1]);

                                    Fight oldFight = FindFight(f);

                                    if(oldFight != null)
                                    {
                                        if(f.fighterA == oldFight.fighterA)
                                        {
                                            f.fighterAResult = oldFight.fighterAResult;
                                            f.fighterBResult = oldFight.fighterBResult;
                                        } 
                                        else
                                        {
                                            f.fighterAResult = oldFight.fighterBResult;
                                            f.fighterBResult = oldFight.fighterAResult;
                                        }
                                    }

                                    tieBreakerFights.Add(f);
                                }

                                tieBreakers.rounds.Add(tieBreakerFights);

                                return false;
                            }
                        }
                    }

                    bracket.fighters.Add((int)dv[i]["ID"]);
                }
            }

            switch (bracket.fighters.Count)
            {
                case 4:
                    bracket.name = "Semi Finals";
                    break;

                case 8:
                    bracket.name = "Quarter Finals";
                    break;

                default:
                    bracket.name = "Top " + bracket.fighters.Count;
                    break;
            }

            //if we want a random bracket, shuffle on the first round of elims
            if (eliminationType == EliminationType.RANDOMISED && eliminations.Count == 0)
            {
                bracket.fighters.Shuffle();
            }

            List<Fight> fights = new List<Fight>();

            for (int i = 0; i < bracket.fighters.Count / 2; i++)
            {
                Fight fight = new Fight();
                fight.fighterA = bracket.fighters[i];
                fight.fighterB = bracket.fighters[bracket.fighters.Count - (i + 1)];

                fights.Add(fight);
            }
            bracket.rounds.Add(fights);
            eliminations.Add(bracket);

            return true;
        }

        public void GenerateFinals()
        {
            if (eliminations.Last().fighters.Count == 4)
            {
                Fight bronzeFight = new Fight();
                Fight goldFight = new Fight();

                Fight fightA = eliminations.Last().rounds.Last().First();
                if (fightA.fighterAResult == Fight.FightResult.WIN)
                {
                    goldFight.fighterA = fightA.fighterA;
                    bronzeFight.fighterA = fightA.fighterB;
                }
                else
                {
                    goldFight.fighterA = fightA.fighterB;
                    bronzeFight.fighterA = fightA.fighterA;
                }

                Fight fightB = eliminations.Last().rounds.Last().Last();
                if (fightB.fighterAResult == Fight.FightResult.WIN)
                {
                    goldFight.fighterB = fightB.fighterA;
                    bronzeFight.fighterB = fightB.fighterB;
                }
                else
                {
                    goldFight.fighterB = fightB.fighterB;
                    bronzeFight.fighterB = fightB.fighterA;
                }

                finals = new List<Fight>() { bronzeFight, goldFight };
            }
        }

        public string AdvanceTournament()
        {
            if (IsComplete())
            {
                switch (stage)
                {
                    case TournamentStage.REGISTRATION:

                        GeneratePools();

                        if (pools == null)
                        {
                            return "Error while generating pool fights - are there enough fighters per pool?";
                        }
                        else
                        {
                            stage = TournamentStage.POOLFIGHTS;
                            return "Pool Fights generated.";
                        }

                    case TournamentStage.POOLFIGHTS:

                        if (poolType == PoolType.SWISSPAIRS && pools.Count < (numberOfRounds * 2))
                        {
                            GenerateSwissPools();
                            return "Next round generated";
                        }
                        else if (GenerateNextEliminationBracket())
                        {
                            stage = TournamentStage.ELIMINATIONS;
                            return "Eliminations generated";
                        }
                        else
                        {
                            stage = TournamentStage.TIEBREAKERS;
                            return "There are fighters tied for qualification. A tie breaker pool has been generated to settle the tie.";
                        }

                    case TournamentStage.TIEBREAKERS:

                        if (GenerateNextEliminationBracket())
                        {
                            stage = TournamentStage.ELIMINATIONS;
                            return "Eliminations generated";
                        }
                        else
                        {
                            return "";
                        }

                    case TournamentStage.ELIMINATIONS:

                        //check if we are moving to the finals
                        if (eliminations.Last().fighters.Count == 4)
                        {
                            stage = TournamentStage.FINALS;
                            GenerateFinals();
                            return "Finals generated";
                        }
                        else
                        {
                            GenerateNextEliminationBracket();
                            return "Next elimination bracket generated";
                        }

                    case TournamentStage.FINALS:


                        stage = Tournament.TournamentStage.CLOSED;
                        return "Tournament closed";

                    default: break;
                }
            }
            else
            {
                return "Fights are not all complete!";
            }

            return "";
        }

        public bool IsComplete()
        {
            foreach (Pool pool in pools)
            {
                if (!pool.IsComplete()) return false;
            }

            foreach (Pool pool in eliminations)
            {
                if (!pool.IsComplete()) return false;
            }

            foreach (Fight fight in finals)
            {
                if (!fight.IsComplete()) return false;
            }

            return true;
        }
    }

    [Serializable]
    public class Pool
    {
        public List<int> fighters = new List<int>();
        public List<List<Fight>> rounds = new List<List<Fight>>();
        public string name;

        public Pool()
        {

        }

        public List<Fight> GenerateSwissRound(Tournament tournament)
        {
            List<Fight> round = new List<Fight>();

            int offset = 0;

            while (round.Count == 0)
            {
                bool breakout = false;

                if (offset >= fighters.Count)
                {
                    //shit... what now?
                    return null; //???? I think we've got this...
                }

                //clone the pool fighter list so we don't remove from the master list
                List<int> roundFighters = new List<int>();
                roundFighters.AddRange(fighters);

                //if there are an odd number of fighters in this pool
                if (roundFighters.Count % 2 == 1)
                {
                    int oddFightIndex = roundFighters.Count - 1;

                    for (int i = roundFighters.Count - 1; i > -1; i--)
                    {
                        List<Fight> fighterFights = tournament.GetPoolFightsByFighter(roundFighters[i]);

                        bool hasHadOddFight = false;

                        foreach (Fight f in fighterFights)
                        {
                            if (f.oddFight)
                            {
                                hasHadOddFight = true;
                                break;
                            }
                        }

                        if (!hasHadOddFight)
                        {
                            oddFightIndex = i;
                            break;
                        }
                    }

                    Fight fight = new Fight(roundFighters[oddFightIndex], int.MaxValue);
                    fight.oddFight = true;
                    round.Add(fight);
                    roundFighters.Remove(fight.fighterA);
                }

                

                for (int l = 0; l < roundFighters.Count;)
                {
                    int opponent = l;

                    int tries = 0;
                    do
                    {
                        opponent += (1 + offset);

                        if (opponent >= roundFighters.Count) opponent -= roundFighters.Count;

                        tries++;

                        //start again if we fuck up too much
                        if (tries > ConfigValues.fightGenerationRetryLimit || opponent >= roundFighters.Count || opponent == l)
                        {
                            round.Clear();
                            offset++;
                            breakout = true;
                            break;
                        }
                    }
                    //ensure the fight hasn't happened already
                    while (tournament.HasFightHappenedAlready(new Fight(roundFighters[l], roundFighters[opponent])) && opponent != l);

                    if (breakout) break;

                    if (opponent < roundFighters.Count && opponent != l)
                    {
                        Fight fight = new Fight(roundFighters[l], roundFighters[opponent]);
                        round.Add(fight);
                        roundFighters.Remove(fight.fighterA);
                        roundFighters.Remove(fight.fighterB);
                    }
                }
            }

            rounds.Add(round);
            return round;
        }

        public List<Fight> GenerateRound()
        {
            List<Fight> round = new List<Fight>();

            //clone the pool fighter list so we don't remove from the master list
            List<int> roundFighters = new List<int>();
            roundFighters.AddRange(fighters);
            Helpers.Shuffle(roundFighters);

            int? prevFighterA = null;
            int? prevFighterB = null;

            int k = rounds.Count;
            if (k > 0)
            {
                prevFighterA = rounds[k - 1][rounds[k - 1].Count - 1].fighterA;
                prevFighterB = rounds[k - 1][rounds[k - 1].Count - 1].fighterB;

                while (roundFighters[0] == prevFighterA || roundFighters[0] == prevFighterB)
                {
                    int f = roundFighters[0];
                    roundFighters.RemoveAt(0);
                    roundFighters.Add(f);
                }
            }

            //don't increment l because we will be removing from the list while iterating anyway
            for (int l = 0; l < roundFighters.Count;)
            {
                int opponent = 0;

                //if there is more than one fighter in this round, generate a normal fight
                if (roundFighters.Count > 1)
                {
                    int tries = 0;
                    do
                    {
                        opponent = Helpers.rng.Next(l + 1, roundFighters.Count);
                        tries++;

                        //start again if we fuck up too much
                        if (tries > ConfigValues.fightGenerationRetryLimit) return null;
                    }
                    //ensure the fight hasn't happened already, and the fighter isn't fighting themselves (that would be pretty dumb)
                    //also try and make sure that the first fight of a round does not contain either of the fighters from the last fight of the previous round
                    //not always possible; if we fail too many times on that condition, just allow it
                    while (HasFightHappenedAlready(new Fight(roundFighters[l], roundFighters[opponent])) || opponent == l || (prevFighterA != null && prevFighterB != null
                                && (roundFighters[opponent] == prevFighterA || roundFighters[opponent] == prevFighterB)
                                && round.Count == 0 && roundFighters.Count > 4 && tries < (ConfigValues.fightGenerationRetryLimit / 2)));

                    Fight fight = new Fight(roundFighters[l], roundFighters[opponent]);
                    round.Add(fight);
                    roundFighters.Remove(fight.fighterA);
                    roundFighters.Remove(fight.fighterB);
                }
                //odd fight if only one fighter left - find a fight from the pool which has not happened yet
                else
                {
                    int tries = 0;
                    do
                    {
                        opponent = Helpers.rng.Next(l + 1, fighters.Count);
                        tries++;

                        //start again if we fuck up too much
                        if (tries > ConfigValues.fightGenerationRetryLimit) return null;
                    }
                    //ensure the fight hasn't happened already, and the fighter isn't fighting themselves, and the opponent was not in the last fight
                    while (HasFightHappenedAlready(new Fight(roundFighters[l], fighters[opponent])) || roundFighters[l] == fighters[opponent] || round.Last().fighterA == fighters[opponent] || round.Last().fighterB == fighters[opponent]);

                    Fight fight = new Fight(roundFighters[l], fighters[opponent]);
                    fight.oddFight = true;
                    round.Add(fight);
                    roundFighters.Remove(fight.fighterA);
                }
            }

            rounds.Add(round);

            return round;
        }

        public bool HasFightHappenedAlready(Fight newFight)
        {
            return (FindFight(newFight) != null);
        }

        public Fight FindFight(Fight newFight)
        {
            foreach (List<Fight> round in rounds)
            {
                foreach (Fight fight in round)
                {
                    if (fight.Equals(newFight)) return fight;
                }
            }
            return null;
        }

        public bool IsComplete()
        {
            foreach (List<Fight> round in rounds)
            {
                foreach (Fight fight in round)
                {
                    if (!fight.IsComplete()) return false;
                }
            }

            return true;
        }
    }

    [Serializable]
    public class Fight
    {
        public enum FightResult { PENDING = 0, WIN = 1, DRAW = 2, LOSS = 3, DQ = 4 };

        public Guid fightID;
        public int fighterA;
        public int fighterB;
        public int doubleCount;
        public FightResult fighterAResult;
        public FightResult fighterBResult;
        public bool oddFight = false;

        public Fight()
        {
            fighterAResult = FightResult.PENDING;
            fighterBResult = FightResult.PENDING;
            fightID = Guid.NewGuid();
        }

        public Fight(int a, int b)
        {
            fighterA = a;
            fighterB = b;
            fighterAResult = FightResult.PENDING;
            fighterBResult = FightResult.PENDING;
            fightID = Guid.NewGuid();
        }

        public bool Equals(Fight obj)
        {
            //if the two fighters are the same, the fight is equal regardless of order
            if (fighterA == obj.fighterA || fighterA == obj.fighterB)
            {
                if (fighterB == obj.fighterA || fighterB == obj.fighterB)
                {
                    return true;
                }
            }
            return false;
        }

        public bool IsComplete()
        {
            return (fighterAResult != FightResult.PENDING && fighterBResult != FightResult.PENDING);
        }
    }

    [Serializable]
    public class Fighter
    {
        public int id;
        public string name;

        //supplemental information, may or may not be used
        public string club;
        public string country;

        //excel values
        public int[] scoreCellRef;
        public string scoreFormula = "";
        public int[] doubleCellRef;
        public string doubleFormula = "";

        public Fighter() { }

        public Fighter(int fighterId, string fighterName)
        {
            id = fighterId;
            name = fighterName;
        }

        public override string ToString()
        {
            return name;
        }
    }
}
