using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using Autodesk.Revit.DB.Analysis;
using Autodesk.Revit.DB.Mechanical;

namespace RuleCheck
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class Class1 : IExternalCommand
    {
        const int ROOMID = -2000160;
        const int MAX_NUM = 999999999;
        const double DOOR_HEIGHT = 2;
        const double SINGLE_WIDTH = 2.6665;
        const double DOUBLE_WIDTH_MIN = 5.33333;
        const double DOUBLE_WIDTH_MAX = 8;
        const double CEILING_HEIGHT = 6.6665;
        const double RAMP_SLOPE = 12.5;
        const double CORR_WID = 9.0;
        public struct Dist
        {
            public double length;
            public int pre;
        }
        //public static readonly string[] ROOMFORBID = new string[1] { "kitchen" };

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            //Get application and documnet objects
            UIApplication uiapp = commandData.Application;
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Document doc = uiapp.ActiveUIDocument.Document;
            Selection selection = uidoc.Selection;
            ICollection<ElementId> selectedIds = uidoc.Selection.GetElementIds();
            var EleIgnored = new List<ElementId>();
            var levelid = ViewLevel(doc);
            var rooms = GetRoomsOnLevel(doc, levelid);
            var RoomIds = rooms.ToList();
            string debug = "";


            CeilingCheck(doc, selectedIds);

            //RoomNameTag(doc);
            var RoomForbid = GetForbidRoom(doc, RoomIds);
            var tmp = Colordoor(doc, RoomForbid);
            var tmp1 = GetPointsInRoom(doc, uiapp, rooms, RoomForbid);
            var rel = Graph(doc, RoomForbid, tmp1);
            Report(rel, doc, RoomForbid);
            DeleteBlock(doc, tmp);

            CheckRamp(doc, uiapp);

            CheckCorridor(doc);

            CheckDoor(doc);
            

            return Result.Succeeded;
        }
        private void Report(KeyValuePair<List<Room>, List<double>> result, Document doc, IList<ElementId> RoomForbid)
        {
            using (Transaction trans = new Transaction(doc))
            {
                trans.Start("Report");
                var allRooms = result.Key;
                var Distance = result.Value;
                double dis;
                string finalReport = "";
                for (int i = 0; i < allRooms.Count; i++)
                {
                    Room room = allRooms[i];
                    if (RoomForbid.Contains(room.Id)) continue;
                    dis = Distance[i];
                    finalReport = "";
                    if (dis > (MAX_NUM - 1)) finalReport += "No egress path!";
                    else
                    {
                        string tmp = dis.ToString();
                        string[] rel = tmp.Split('.');

                        finalReport += "Travel:"+rel[0] + "." + rel[1].Substring(0, 2) + "ft";
                    }
                    AddTag(doc, room.Id, finalReport);
                }
                trans.Commit();
            }
            
        }
        //public KeyValuePair<List<ElementId>, List<double>> TravelDis(Document doc, ICollection<ElementId> selectedIds, List<ElementId> RoomsForbid) //distances of all rooms on current level to nearest exit
        //{

        //    View currentView = doc.ActiveView;

        //    //door location
        //    var doors = new List<ElementId>();
        //    doors = GetExits(doc);
        //    var doors_loc = new List<XYZ>();
        //    foreach (ElementId id in doors)
        //    {
        //        Element door = doc.GetElement(id);
        //        LocationPoint loc = door.Location as LocationPoint;
        //        XYZ xyz = loc.Point;
        //        doors_loc.Add(xyz);
        //    }
        //    //room location
        //    var levelid = ViewLevel(doc);
        //    var rooms = GetRoomsOnLevel(doc, levelid);
        //    var final_rel = new List<double>();
        //    var rooms_loc = CenterOfRoom(doc, rooms);

        //    //TaskDialog.Show("Revit", doors_loc.Count.ToString());
        //    //TaskDialog.Show("Revit", rooms_loc.Count.ToString());
        //    var Exit2Door = new List<XYZ>();

        //    using (TransactionGroup transGroup = new TransactionGroup(doc))
        //    {
        //        transGroup.Start("group start");
        //        using (Transaction trans_del = new Transaction(doc))
        //        {
        //            trans_del.Start("Del");
        //            foreach (ElementId id in RoomsForbid)
        //            {
        //                Element temp = doc.GetElement(id);
        //                DeleteDoorsOfRoom(doc, id);
        //            }
        //            trans_del.Commit();
        //        }
        //        using (Transaction trans = new Transaction(doc))
        //        {
        //            if (trans.Start("Path") == TransactionStatus.Started)
        //            {


        //                //PathOfTravel.CreateMapped(currentView, rooms_loc, doors_loc);

        //                //try to find the shortest path to the exits(one of)
        //                //var ig = new List<ElementId>();
        //                var settings = RouteAnalysisSettings.GetRouteAnalysisSettings(doc);
        //                //foreach (ElementId id in selectedIds)
        //                //{
        //                //    Element temp = doc.GetElement(id);
        //                //    ig.Add(temp.Category.Id);
        //                //}
        //                settings.SetIgnoredCategoryIds(selectedIds);
        //                foreach (XYZ r in rooms_loc)
        //                {
        //                    double temp_len = 10000000;
        //                    XYZ temp_loc = null;
        //                    int cnt = 0;
        //                    foreach (XYZ d in doors_loc)
        //                    {
        //                        PathOfTravel path = PathOfTravel.Create(currentView, r, d);
        //                        if (path == null) continue;
        //                        IList<Curve> p = path.GetCurves();
        //                        if (temp_len >= calDis(p))
        //                        {
        //                            temp_loc = d;
        //                            temp_len = calDis(p);
        //                        }

        //                    }
        //                    Exit2Door.Add(temp_loc);
        //                }
        //                trans.RollBack();

        //                //TaskDialog taskdialog = new TaskDialog("Revit");
        //                //taskdialog.MainContent = "Click [OK] to commot and click [cancel] to roll back";
        //                //TaskDialogCommonButtons buttons = TaskDialogCommonButtons.Ok | TaskDialogCommonButtons.Cancel;
        //                //taskdialog.CommonButtons = buttons;
        //                //if (TaskDialogResult.Ok == taskdialog.Show())
        //                //{
        //                //    if (TransactionStatus.Committed != trans.Commit()) {
        //                //        TaskDialog.Show("Fail", "Trans can not be committed");
        //                //    }
        //                //}
        //                //else {
        //                //    trans.RollBack();
        //                //}
        //            }
        //        }

        //        var RoomsPoint = rooms_loc;

        //        using (Transaction trans2 = new Transaction(doc))
        //        {
        //            if (trans2.Start("Path_final") == TransactionStatus.Started)
        //            {

        //                var settings = RouteAnalysisSettings.GetRouteAnalysisSettings(doc);

        //                settings.SetIgnoredCategoryIds(selectedIds);
        //                for (int i = 0; i < RoomsPoint.Count; i++)
        //                {
        //                    XYZ d = Exit2Door[i];
        //                    XYZ r = RoomsPoint[i];
        //                    Room temp_room = doc.GetRoomAtPoint(r);
        //                    double halfDia = calHalfDia(temp_room);
        //                    if (r == null || d == null)
        //                    {
        //                        final_rel.Add(MAX_NUM);
        //                        continue;
        //                    };
        //                    IList<Curve> path = PathOfTravel.Create(currentView, r, d).GetCurves();
        //                    final_rel.Add(calDis(path));
        //                }
        //                trans2.Commit();
        //            }
        //        }
        //        transGroup.Assimilate();
        //    }
        //    var allRoomName = new List<ElementId>();
        //    foreach (Room r in rooms) allRoomName.Add(r.Id);
        //    return new KeyValuePair<List<ElementId>, List<double>>(allRoomName, final_rel);

        //}
        public IEnumerable<Room> GetRoomsOnLevel(Document doc, ElementId idLevel) //get all rooms on current level
        {
            return new FilteredElementCollector(doc)
              .WhereElementIsNotElementType()
              .OfClass(typeof(SpatialElement))
              .Where(e => e.GetType() == typeof(Room))
              .Where(e => e.LevelId.IntegerValue.Equals(
               idLevel.IntegerValue))
              .Cast<Room>();
        }

        public IEnumerable<Element> GetSpacesOnLevel(Document doc, ElementId idLevel) //get all rooms on current level
        {
            return new FilteredElementCollector(doc)
              .WhereElementIsNotElementType()
              .OfClass(typeof(SpatialElement))
              .Where(e => e.Category.Id.IntegerValue == (int)BuiltInCategory.OST_MEPSpaces)
              .Where(e => e.LevelId.IntegerValue.Equals(
               idLevel.IntegerValue));
        }
        public Double calDis(IList<Curve> p) //cal the lenght of a travel path
        {
            double rel = 0;
            foreach (Curve c in p)
            {
                rel += c.Length;
            }
            return rel;
        }
        public ElementId ViewLevel(Document doc)//get view of the current level
        {
            View active = doc.ActiveView;
            ElementId levelId = null;

            Parameter level = active.LookupParameter("Associated Level");

            FilteredElementCollector lvlCollector = new FilteredElementCollector(doc);
            ICollection<Element> lvlCollection = lvlCollector.OfClass(typeof(Level)).ToElements();

            foreach (Element l in lvlCollection)
            {
                Level lvl = l as Level;
                if (lvl.Name == level.AsString())
                {
                    levelId = lvl.Id;
                    //TaskDialog.Show("test", lvl.Name + "\n"  + lvl.Id.ToString());
                }
            }
            return levelId;

        }
        public List<ElementId> GetExits(Document doc) //get all elements which are exits
        {
            ElementClassFilter familyInstancefilter = new ElementClassFilter(typeof(FamilyInstance));
            ElementCategoryFilter doorfilter = new ElementCategoryFilter(BuiltInCategory.OST_Doors);
            LogicalAndFilter doorInstancefilter = new LogicalAndFilter(familyInstancefilter, doorfilter);
            FilteredElementCollector col = new FilteredElementCollector(doc);
            ICollection<ElementId> doors = col.WherePasses(doorInstancefilter).ToElementIds();
            var rel = new List<ElementId>();
            foreach (ElementId id in doors)
            {
                Element door = doc.GetElement(id);
                FamilyInstance doorfam = door as FamilyInstance;
                Parameter temp = doorfam.Symbol.LookupParameter("Function");
                if (temp.AsValueString() == "Exterior")
                {
                    rel.Add(id);
                }
            }
            //TaskDialog.Show("Revit", rel.Count.ToString());
            return rel;
        }
        public void DeleteEle(Document document, Element element)
        {
            // Delete an element via its id
            ElementId elementId = element.Id;
            ICollection<ElementId> deletedIdSet = document.Delete(elementId);

            if (0 == deletedIdSet.Count)
            {
                throw new Exception("Deleting the selected element in Revit failed.");
            }

            String prompt = "The selected element has been removed and ";
            prompt += deletedIdSet.Count - 1;
            prompt += " more dependent elements have also been removed.";

            // Give the user some information
            //TaskDialog.Show("Revit", prompt);
        }
        public void DeleteDoorsOfRoom(Document doc, ElementId dangerRoom)
        {
            ElementClassFilter familyInstancefilter = new ElementClassFilter(typeof(FamilyInstance));
            ElementCategoryFilter doorfilter = new ElementCategoryFilter(BuiltInCategory.OST_Doors);
            LogicalAndFilter doorInstancefilter = new LogicalAndFilter(familyInstancefilter, doorfilter);
            FilteredElementCollector col = new FilteredElementCollector(doc);
            ICollection<ElementId> doors = col.WherePasses(doorInstancefilter).ToElementIds();
            var rel = new List<ElementId>();
            string debug = "";

            foreach (ElementId id in doors)
            {
                Element door = doc.GetElement(id);
                FamilyInstance doorfam = door as FamilyInstance;
                Room temp1 = doorfam.FromRoom;
                Room temp2 = doorfam.ToRoom;
                if (temp1 != null && temp1.Id == dangerRoom)
                {
                    DeleteEle(doc, door);
                    continue;
                }
                if (temp2 != null && temp2.Id == dangerRoom)
                {
                    DeleteEle(doc, door);
                    continue;
                }
            }

            //TaskDialog.Show("Revit", debug);

        }
        public IList<XYZ> CalPointOfRooms(Document doc, IEnumerable<Room> rooms, List<XYZ> Exit2Door, ICollection<ElementId> eleIg)
        {
            var rel = new List<XYZ>();
            using (Transaction trans = new Transaction(doc))
            {
                if (trans.Start("Path") == TransactionStatus.Started)
                {
                    int count = 0;
                    var settings = RouteAnalysisSettings.GetRouteAnalysisSettings(doc);
                    settings.SetIgnoredCategoryIds(eleIg);
                    foreach (Room room in rooms)
                    {
                        var exit = Exit2Door[count];
                        BoundingBoxXYZ box = room.get_BoundingBox(null);
                        Transform trf = box.Transform;
                        XYZ min_xyz = box.Min;
                        XYZ max_xyz = box.Max;
                        XYZ minInCoor = trf.OfPoint(min_xyz);
                        XYZ maxInCoor = trf.OfPoint(max_xyz);
                        List<XYZ> temp = new List<XYZ>();
                        temp.Add(new XYZ(minInCoor.X, maxInCoor.Y, minInCoor.Z));
                        temp.Add(new XYZ(minInCoor.Y, maxInCoor.X, minInCoor.Z));
                        temp.Add(new XYZ(maxInCoor.X, minInCoor.Y, minInCoor.Z));
                        temp.Add(new XYZ(maxInCoor.Y, minInCoor.X, minInCoor.Z));

                        XYZ final = null;
                        double final_dis = MAX_NUM;
                        foreach (XYZ r in temp)
                        {
                            if (!room.IsPointInRoom(r)) continue;
                            PathOfTravel path = PathOfTravel.Create(doc.ActiveView, r, exit);
                            if (path == null) continue;
                            double dis = calDis(path.GetCurves());
                            if (dis < final_dis)
                            {
                                final_dis = dis;
                                final = r;
                            }
                        }
                        if (final == null)
                        {
                            LocationPoint loc = room.Location as LocationPoint;
                            XYZ xyz = loc.Point;
                            rel.Add(xyz);
                        }
                        else
                        {
                            rel.Add(final);
                        }
                    }
                    trans.RollBack();
                }
            }

            //foreach (Room r in rooms)
            //{
            //    LocationPoint loc = r.Location as LocationPoint;
            //    XYZ xyz = loc.Point;
            //    rel.Add(xyz);
            //}
            return rel;
        }
        public IList<XYZ> CenterOfRoom(Document doc, IEnumerable<Room> rooms)
        {
            var rel = new List<XYZ>();
            foreach (Room r in rooms)
            {
                LocationPoint loc = r.Location as LocationPoint;
                XYZ xyz = loc.Point;
                rel.Add(xyz);
            }
            return rel;
        }
        public double calHalfDia(Room room)
        {
            BoundingBoxXYZ box = room.get_BoundingBox(null);
            Transform trf = box.Transform;
            XYZ min_xyz = box.Min;
            XYZ minInCoor = trf.OfPoint(min_xyz);
            XYZ max_xyz = box.Max;
            XYZ maxInCoor = trf.OfPoint(max_xyz);
            XYZ point = new XYZ(maxInCoor.X, maxInCoor.Y, minInCoor.Z);
            return minInCoor.DistanceTo(point) / 2;
        }
        public KeyValuePair<List<Room>, List<double>> Graph(Document doc, IList<ElementId> RoomForbid, KeyValuePair<List<Room>, List<List<XYZ>>> Points)
        {
            var levelid = ViewLevel(doc);
            var rooms = GetRoomsOnLevel(doc, levelid);
            var doors = GetAllDoors(doc, levelid);
            var exits = GetExits(doc);
            var RoomIDs = new List<ElementId>();
            var DoorIDs = new List<ElementId>();
            var AllIDs = new List<ElementId>();
            foreach (ElementId d in doors)
            {
                DoorIDs.Add(d);
                AllIDs.Add(d);
            }
            foreach (Room r in rooms)
            {
                RoomIDs.Add(r.Id);
                AllIDs.Add(r.Id);
            }


            var mat_dim = DoorIDs.Count;
            int[,] mat = new int[100, 100];
            for (int i = 0; i < 100; i++)
                for (int j = 0; j < 100; j++)
                    mat[i, j] = 0;

            foreach (ElementId id1 in DoorIDs)
            {
                int count = 0;
                Element door1 = doc.GetElement(id1);
                FamilyInstance doorfam1 = door1 as FamilyInstance;
                Room temp1 = doorfam1.FromRoom;
                Room temp2 = doorfam1.ToRoom;
                int index1 = DoorIDs.FindIndex(a => a.IntegerValue == id1.IntegerValue);
                foreach (ElementId id2 in DoorIDs)
                {
                    Element door2 = doc.GetElement(id2);
                    FamilyInstance doorfam2 = door2 as FamilyInstance;
                    Room temp1_1 = doorfam2.FromRoom;
                    Room temp2_1 = doorfam2.ToRoom;
                    int index2 = DoorIDs.FindIndex(a => a.IntegerValue == id2.IntegerValue);
                    if (temp1 != null && temp1_1 != null && temp1.Id.IntegerValue == temp1_1.Id.IntegerValue && !(RoomForbid.Contains(temp1.Id)))
                    {
                        mat[index1,index2] = 1; count++;
                        continue;
                    }
                    if (temp1 != null && temp2_1 != null && temp1.Id.IntegerValue == temp2_1.Id.IntegerValue && !(RoomForbid.Contains(temp1.Id)))
                    {
                        mat[index1, index2] = 1; count++;
                        continue;
                    }
                    if (temp2 != null && temp2_1 != null && temp2.Id.IntegerValue == temp2_1.Id.IntegerValue && !(RoomForbid.Contains(temp2.Id)))
                    {
                        mat[index1, index2] = 1; count++;
                        continue;
                    }
                    if (temp2 != null && temp1_1 != null && temp2.Id.IntegerValue == temp1_1.Id.IntegerValue && !(RoomForbid.Contains(temp2.Id)))
                    {
                        mat[index1, index2] = 1; count++;
                        continue;
                    }
                }

            }

            var RoomLocs = new List<XYZ>();
            var DoorLocs = new List<XYZ>();
            var AllLocs = new List<XYZ>();
            var LocsForbid = new List<XYZ>();
            foreach (ElementId id in DoorIDs)
            {
                Element r = doc.GetElement(id);
                LocationPoint loc = r.Location as LocationPoint;
                XYZ xyz = loc.Point;
                DoorLocs.Add(xyz);
                AllLocs.Add(xyz);
            }
            foreach (ElementId id in RoomIDs)
            {
                Element r = doc.GetElement(id);
                LocationPoint loc = r.Location as LocationPoint;
                XYZ xyz = loc.Point;
                RoomLocs.Add(xyz);
                AllLocs.Add(xyz);
                if (RoomForbid.Contains(id))
                {
                    LocsForbid.Add(xyz);
                }
            }
            double[,] ajm = new double[100, 100];
            for (int i = 0; i < mat_dim; i++)
                for (int j = 0; j < mat_dim; j++)
                    ajm[i, j] = -1;
            IList<Curve>[,] pathMap = new IList<Curve>[mat_dim, mat_dim];
            using (Transaction trans = new Transaction(doc))
            {
                trans.Start("CAL");
                int offset = DoorIDs.Count;
                View view = doc.ActiveView;
                for (int i = 0; i < mat_dim; i++)
                    for (int j = 0; j < mat_dim; j++)
                    {
                        if (mat[i, j] == 0) continue;
                        if (j == i) continue;
                        PathOfTravel p = PathOfTravel.Create(view, DoorLocs[i], DoorLocs[j]);
                        if (p == null)
                        {

                            continue;
                        }
                        var crs = p.GetCurves();
                        pathMap[i, j] = crs;
                        ajm[i, j] = calDis(crs);
                        ajm[j, i] = ajm[i, j];
                    }
                trans.RollBack();
            }

            //for (int i = DoorIDs.Count; i < mat_dim; i++) {
            //    int tmp = 0;
            //    for (int j = 0; j < mat_dim; j++)
            //        if (ajm[i, j] > 0) tmp++;
            //    if(tmp==0) TaskDialog.Show("Revit", RoomIDs[i-DoorIDs.Count].ToString());
            //}

            //foreach (ElementId fid in RoomForbid)
            //{
            //    Element tmp = doc.GetElement(fid);
            //    if (!tmp.Category.Name.ToLower().Contains("room")) continue;
            //    int index = AllIDs.FindIndex(a => a.IntegerValue == fid.IntegerValue);
            //    for (int i = 0; i < mat_dim; i++)
            //    {
            //        ajm[index, i] = -1;
            //        ajm[i, index] = -1;
            //    }
            //    TaskDialog.Show("revit", "delete a room");
            //}
            for (int i = 0; i < mat_dim; i++)
                for (int j = 0; j < mat_dim; j++)
                {
                    if (i == j)
                    {
                        ajm[i, j] = 0;
                        continue;
                    }
                    if (ajm[i, j] < 0) ajm[i, j] = MAX_NUM;

                }
            //string ttt = "";
            //for (int i = 0; i < AllIDs.Count; i++) {
            //    for (int j = 0; j < AllIDs.Count; j++)
            //        ttt += ajm[i, j].ToString() + "  ";

            //    ttt += "\n";
            //}
            //TaskDialog.Show("revit", ttt);



            var dis = GetFloyd(ajm, mat_dim);
            var final_rel = new List<double>();
            var final_des = new List<int>();
            foreach (ElementId rid in DoorIDs)
            {
                double len = MAX_NUM;
                int des_node = -1;
                int x = DoorIDs.FindIndex(a => a.IntegerValue == rid.IntegerValue);
                foreach (ElementId did in exits)
                {
                    int y = DoorIDs.FindIndex(a => a.IntegerValue == did.IntegerValue);
                    double tmp_len = dis[x, y].length;
                    if (len >= tmp_len)
                    {
                        len = tmp_len;
                        des_node = y;
                    }
                }
                final_rel.Add(len);
                final_des.Add(des_node);
            }

            var Final_path = new List<List<int>>();
            for (int i = 0; i < DoorIDs.Count; i++)
            {
                var rid = DoorIDs[i];
                if (final_rel[i] > MAX_NUM - 1)
                {
                    Final_path.Add(null);
                    continue;
                }
                var nodes = new List<int>();
                var dst = final_des[i];
                int x = DoorIDs.FindIndex(a => a.IntegerValue == rid.IntegerValue);
                nodes.Add(dst);
                int pre = dis[x, dst].pre;
                while (true)
                {
                    nodes.Add(pre);
                    if (pre == x)
                    {
                        break;
                    }
                    pre = dis[x, pre].pre;
                }
                nodes.Reverse();
                Final_path.Add(nodes);
            }

            var result = new List<double>();

            using (Transaction trans1 = new Transaction(doc))
            {
                trans1.Start("Correction");
                View view = doc.ActiveView;
                foreach (List<int> path in Final_path)
                {
                    if (path == null)
                    {
                        result.Add(MAX_NUM);
                        continue;
                    }
                    XYZ startpoint = DoorLocs[path[0]];
                    XYZ endpoint;
                    double distance = 0;
                    for (int i = 0; i < path.Count; i++)
                    {
                            endpoint = DoorLocs[path[i]];
                            if (endpoint == null || startpoint == null) TaskDialog.Show("Error", DoorIDs[path[i]].ToString());
                            if (path[i] == path[0]) continue;
                            PathOfTravel p = PathOfTravel.Create(view, startpoint, endpoint);
                            distance += calDis(p.GetCurves());
                            startpoint = endpoint;
                    }
                    result.Add(distance);
                }
                trans1.Commit();
            }

            var finalDis = new List<double>();
            var finalPre = new List<ElementId>();
            var finalPoints = new List<XYZ>();
            var roomlist = Points.Key;

 
            using (Transaction tran_final_0 = new Transaction(doc))
            {
                tran_final_0.Start("Determine the door");
                
                var pointInRoom = Points.Value;
                View view = doc.ActiveView;
                int pcnt = 0;
                //Room rtmp = roomlist[0];
                //XYZ xyz1 = pointInRoom[0][0];
                //foreach (ElementId id in DoorIDs)
                //{
                //    FamilyInstance door = doc.GetElement(id) as FamilyInstance;
                //    if (door.ToRoom.Id.IntegerValue == rtmp.Id.IntegerValue || door.FromRoom.Id.IntegerValue == rtmp.Id.IntegerValue)
                //    {
                //        int index = DoorIDs.FindIndex(a => a.IntegerValue == id.IntegerValue);
                //        PathOfTravel.Create(view, xyz1, DoorLocs[index]).GetCurves();
                //        TaskDialog.Show("Error", "shithihsfd");
                //        break;
                //    }
                //}
                for (int i = 0; i < roomlist.Count; i++)
                {
                    var room = roomlist[i];
                    var points = pointInRoom[i];
                    var doorsToroom = new List<ElementId>();
                    foreach (ElementId id in DoorIDs)
                    {
                        FamilyInstance door = doc.GetElement(id) as FamilyInstance;

                        if (door.ToRoom != null && door.ToRoom.Id.IntegerValue == room.Id.IntegerValue)
                        {
                            doorsToroom.Add(id);
                        }
                        if (door.FromRoom != null && door.FromRoom.Id.IntegerValue == room.Id.IntegerValue)
                        {
                            doorsToroom.Add(id);
                        }
                    }
                    int doorid = -1;
                    double shortest_door = MAX_NUM;
                    foreach (ElementId did in doorsToroom)
                    {
                        int index = DoorIDs.FindIndex(a => a.IntegerValue == did.IntegerValue);
                        if (index < 0 || index >= result.Count) {
                            TaskDialog.Show("INdex error", index.ToString());
                        }
                        if (result[index] < shortest_door-2)
                        {
                            doorid = index;
                            shortest_door = result[index];
                        }
                    }
                    
                    double dis_final = 0;
                    ElementId des_door = null;
                    XYZ finalPoint = null;
                    if (doorid == -1)
                    {
                        dis_final = MAX_NUM;
                        des_door = null;
                        finalPoint = null;

                    }
                    else
                    {
                        foreach (XYZ xyz1 in points)
                        {
                            if (doorid < 0 || doorid >= DoorLocs.Count)
                                TaskDialog.Show("Error", "Doorid" + doorid.ToString());
                            XYZ xyz2 = DoorLocs[doorid];
                            if (xyz1.DistanceTo(xyz2) < 10) continue;
                            PathOfTravel p = PathOfTravel.Create(view, xyz1, xyz2);
                            if (p == null) continue;
                            if (calDis(p.GetCurves()) > dis_final)
                            {
                                dis_final = calDis(p.GetCurves());
                                des_door = DoorIDs[doorid];
                                finalPoint = xyz1;
                            }


                        }
                    }
                    finalDis.Add(dis_final);
                    finalPre.Add(des_door);
                    finalPoints.Add(finalPoint);

                }
                tran_final_0.RollBack();
            }
            string debug = "";
            for (int i = 0; i < finalPoints.Count; i++)
            {
                if (finalPoints[i] == null)
                    debug += "null  ";
                else
                    debug += finalPoints[i].ToString() + "    ";
                if (finalPre[i] == null)
                    debug += "null\n";
                else
                    debug += finalPre[i].ToString() + "\n";
            }
            var finalResult = new List<double>();
            using (Transaction tran_final_1 = new Transaction(doc))
            {

                View view = doc.ActiveView;
                tran_final_1.Start("Final");

                for (int i = 0; i < finalPoints.Count; i++)
                {
                    if (finalPre[i] == null)
                    {
                        finalResult.Add(MAX_NUM);
                        continue;
                    }
                    XYZ start = finalPoints[i];
                    int index = DoorIDs.FindIndex(a => a.IntegerValue == finalPre[i].IntegerValue);

                    XYZ end = DoorLocs[index];
                    PathOfTravel p = PathOfTravel.Create(view, start, end);
                    double dist = calDis(p.GetCurves());
                    finalResult.Add(finalDis[i] + result[index]); 
                }
                tran_final_1.Commit();
            }

         

            return new KeyValuePair<List<Room>, List<double>>(roomlist, finalResult);
        }
        public IList<ElementId> GetAllDoors(Document doc, ElementId levelid)
        {
            ElementClassFilter familyInstancefilter = new ElementClassFilter(typeof(FamilyInstance));
            ElementCategoryFilter doorfilter = new ElementCategoryFilter(BuiltInCategory.OST_Doors);
            LogicalAndFilter doorInstancefilter = new LogicalAndFilter(familyInstancefilter, doorfilter);
            FilteredElementCollector col = new FilteredElementCollector(doc);
            ICollection<ElementId> doors = col.WherePasses(doorInstancefilter).ToElementIds();
            var rel = new List<ElementId>();
            foreach (ElementId id in doors)
            {
                Element door = doc.GetElement(id);
                if (door.LevelId == levelid) rel.Add(id);
            }
            return rel;
        }
        public Dist[,] GetFloyd(double[,] G, int N)
        {
            int i, j, v;
            Dist[,] D = new Dist[N, N];
            for (i = 0; i < N; i++)
            {
                for (j = 0; j < N; j++)
                {
                    if (i == j)
                    {
                        D[i, j].length = 0;
                        D[i, j].pre = i;
                    }
                    else
                    {
                        if (G[i, j] < MAX_NUM - 1)
                        {
                            D[i, j].length = G[i, j];
                            D[i, j].pre = i;
                        }
                        else
                        {
                            D[i, j].length = MAX_NUM;
                            D[i, j].pre = -1;
                        }
                    }
                }
            }

            for (v = 0; v < N; v++)
            {
                for (i = 0; i < N; i++)
                {
                    for (j = 0; j < N; j++)
                    {
                        if (D[i, j].length > (D[i, v].length + D[v, j].length))
                        {
                            D[i, j].length = D[i, v].length + D[v, j].length;
                            D[i, j].pre = D[v, j].pre;
                        }
                    }
                }
            }

            return D;
        }
        public IList<ElementId> GetForbidRoom(Document doc, IEnumerable<Room> Rooms)
        {
            var rel = new List<ElementId>();
            string path = "D:\\Config.txt";
            var str = "";
            bool flag = false;
            if (!System.IO.File.Exists(path))
            {
                flag = true;    
            }
            else
            {
                string line = "";
                string names = "";
                System.IO.StreamReader file = new System.IO.StreamReader(path);
                while ((line = file.ReadLine()) != null)
                {
                    System.Console.WriteLine(line);
                    var rel_line = line.Split(' ');
                    int tag = Int32.Parse(rel_line[1]);
                    if (tag==1) {
                        foreach (Room r in Rooms)
                        {
                            if (r.Id.IntegerValue == Int32.Parse(rel_line[0]))
                            {
                                rel.Add(r.Id);
                                names += r.Name + "\n";
                            }
                        }
                    }
                }

                file.Close();

                TaskDialog dialog = new TaskDialog("Forbidden Rooms");
                dialog.MainContent = names + "\n" + "Do you need to change forbidden rooms?";
                dialog.CommonButtons = TaskDialogCommonButtons.Yes | TaskDialogCommonButtons.No;
                TaskDialogResult result = dialog.Show();
                if (result == TaskDialogResult.Yes)
                {
                    flag = true;
                }
            }
            if (flag)
            {
                rel = new List<ElementId>();
                foreach (Room room in Rooms)
                {
                    TaskDialog dialog = new TaskDialog("Is the Room Forbidden?");
                    dialog.MainContent = room.Name + "\n" + "Is this room can not be passed?";
                    dialog.CommonButtons = TaskDialogCommonButtons.Yes | TaskDialogCommonButtons.No;

                    TaskDialogResult result = dialog.Show();
                    if (result == TaskDialogResult.Yes)
                    {
                        rel.Add(room.Id);
                        str += room.Id.ToString() + " " + "1" + "\n";
                    }
                    else
                    {
                        str += room.Id.ToString() + " " + "0" + "\n";
                    }
                }
                System.IO.File.WriteAllText(path, str);
            }
            
            return rel;
        }

        public IList<ElementId> CheckDoor(Document doc)
        {
            ElementClassFilter familyInstancefilter = new ElementClassFilter(typeof(FamilyInstance));
            ElementCategoryFilter doorfilter = new ElementCategoryFilter(BuiltInCategory.OST_Doors);
            LogicalAndFilter doorInstancefilter = new LogicalAndFilter(familyInstancefilter, doorfilter);
            FilteredElementCollector col = new FilteredElementCollector(doc);
            ICollection<ElementId> doors = col.WherePasses(doorInstancefilter).ToElementIds();
            View view = doc.ActiveView;

            var rel = new List<ElementId>();
            var relstr = new List<string>();
            foreach (ElementId id in doors)
            {
                string strtmp = "";
                Element door = doc.GetElement(id);
                bool flag = true;
                FamilyInstance fs = door as FamilyInstance;
                string Name = fs.Symbol.Name;
                BoundingBoxXYZ box = door.get_BoundingBox(null);
                Transform trf = box.Transform;
                XYZ min_xyz = box.Min;
                XYZ max_xyz = box.Max;
                XYZ minInCoor = trf.OfPoint(min_xyz);
                XYZ maxInCoor = trf.OfPoint(max_xyz);
                double height = maxInCoor.Z - minInCoor.Z;
                double width;
                if ((maxInCoor.X - minInCoor.X) > (maxInCoor.Y - minInCoor.Y))
                    width = maxInCoor.X - minInCoor.X;
                else
                    width = maxInCoor.Y - minInCoor.Y;
                if (height < DOOR_HEIGHT)
                {
                    flag = false;
                    strtmp += "Door too low";
                }
                if (Name.ToLower().Contains("double"))
                {
                    if (width < DOUBLE_WIDTH_MIN || width > DOUBLE_WIDTH_MAX)
                    {
                        flag = false;
                        strtmp += "Door too narrow or too wide";
                    }

                }
                else
                {
                    if (width < SINGLE_WIDTH)
                    {
                        flag = false;
                        strtmp += "Door too narrow or too wide";
                    }
                }

                if (flag == false)
                {
                    rel.Add(id);
                    relstr.Add(strtmp);
                }
            }



            //if (rel.Count != 0)
            //{
            //    string Error = "";
            //    foreach (ElementId id in rel)
            //    {
            //        Error += id.ToString() + "\n";
            //    }
            //    TaskDialog td = new TaskDialog("Door Error");
            //    td.MainIcon = TaskDialogIcon.TaskDialogIconWarning;
            //    td.MainInstruction = "Error, following doors' height or width don't satisfy!.\n Their ids:";
            //    td.MainContent = Error;
            //    TaskDialogResult tdRes = td.Show();
            //    HighLight(doc, rel);
            //}
            Color(doc, rel,"R");
            WriteRel(new KeyValuePair<List<ElementId>, List<string>>(rel, relstr));
            return rel;
        }

        public void HighLight(Document doc, IList<ElementId> id)
        {
            ICollection<ElementId> ids = new List<ElementId>();

            foreach (ElementId i in id)
                ids.Add(i);

            UIDocument uiDoc = new UIDocument(doc);
            if (ids.Count == 0) return;
            uiDoc.Selection.SetElementIds(ids);

            uiDoc.ShowElements(ids);
        }
        public Result CeilingCheck(Document doc,ICollection<ElementId> roomids) 
        {
            var rooms = new List<Room>();
            foreach (ElementId id in roomids)
            {
                Element room = doc.GetElement(id);
                if (room.Category.Id.IntegerValue == (int)BuiltInCategory.OST_Rooms)
                    rooms.Add(room as Room);
            }
            var elementsInView = GetRoomEle(rooms);
            var rel = new List<ElementId>();
            if (elementsInView.Count >= 1)
            {
                foreach (Element e in elementsInView)
                {
                    string name = e.Name.ToLower();
                    if (name.Contains("window") || name.Contains("door") || name.Contains("wall"))
                        continue;
                    FamilyInstance skylight = e as FamilyInstance;
                    if (skylight == null)
                        continue;
                    try
                    {
                        double line = CalculateLineAboveFloor(doc, skylight);
                        if (line <= CEILING_HEIGHT && line >= 2.25)
                            rel.Add(e.Id);
                    }
                    catch (Exception)
                    {
                    }
                    
                }
            }

            if (rel.Count != 0)
            { 
                var relstr = new List<string>();
                foreach (ElementId id in rel)
                {
                    relstr.Add("Headroom not enough");
                }
                Color(doc, rel,"R");
                WriteRel(new KeyValuePair<List<ElementId>, List<string>>(rel,relstr));
            }

            return Result.Succeeded;
        }

        /// <summary>
        /// Determines the line segment that connects the skylight to the nearest floor.
        /// </summary>
        /// <returns>The line segment.</returns>
        private double CalculateLineAboveFloor(Document doc, FamilyInstance skylight)
        {
            // Find a 3D view to use for the ReferenceIntersector constructor
            FilteredElementCollector collector = new FilteredElementCollector(doc);
            Func<View3D, bool> isNotTemplate = v3 => !(v3.IsTemplate);
            View3D view3D = collector.OfClass(typeof(View3D)).Cast<View3D>().First<View3D>(isNotTemplate);

            // Use the center of the skylight bounding box as the start point.
            if (skylight == null) TaskDialog.Show("Revit", "Error");
            BoundingBoxXYZ box = skylight.get_BoundingBox(view3D);
            XYZ center = box.Min;

            // Project in the negative Z direction down to the floor.
            XYZ rayDirection = new XYZ(0, 0, -1);
            ElementClassFilter filter = new ElementClassFilter(typeof(Floor));

            ReferenceIntersector refIntersector = new ReferenceIntersector(filter, FindReferenceTarget.Face, view3D);
            ReferenceWithContext referenceWithContext = refIntersector.FindNearest(center, rayDirection);

            Reference reference = referenceWithContext.GetReference();
            XYZ intersection = reference.GlobalPoint;

            // Create line segment from the start point and intersection point.
            Line result = Line.CreateBound(center, intersection);
            return result.Length;
        }
        public List<Element> GetRoomEle(IList<Room> rooms)
        {
            List<Element> a = new List<Element>();
            foreach (Room room in rooms)
            {
                
                BoundingBoxXYZ bb = room.get_BoundingBox(null);
               
                Outline outline = new Outline(bb.Min, bb.Max);

                BoundingBoxIntersectsFilter filter
                  = new BoundingBoxIntersectsFilter(outline);
                
                Document doc = room.Document;

                // Todo: add category filters and other
                // properties to narrow down the results

                FilteredElementCollector collector
                  = new FilteredElementCollector(doc)
                    .WhereElementIsNotElementType()
                    .WhereElementIsViewIndependent()
                    .OfClass(typeof(FamilyInstance))
                    .WherePasses(filter);

                int roomid = room.Id.IntegerValue;


                
                foreach (FamilyInstance fi in collector)
                {
                    a.Add(fi as Element);
                }
            }
            return a;
        }
        public KeyValuePair<List<Room>, List<List<XYZ>>> GetPointsInRoom(Document doc, UIApplication uiapp,IEnumerable<Room> rooms,IList<ElementId> RoomForbid)
        {
            String str = "";
            var levelid = ViewLevel(doc);
            var allSpaces = GetSpacesOnLevel(doc, levelid);
            var roomList = new List<Room>();
            var roomPoint = new List<List<XYZ>>();
            var ForbidRoom = new List<Int32>();
            foreach (ElementId id in RoomForbid)
                ForbidRoom.Add(id.IntegerValue);
            foreach (Room ele in rooms)
            {
                if (!ForbidRoom.Contains(ele.Id.IntegerValue))
                    roomList.Add(ele);
            }
            for (int i = 0; i < roomList.Count; i++)
            {
                roomPoint.Add(new List<XYZ>());
            }
            Options geomOption = uiapp.Application.Create.NewGeometryOptions();
            if (null != geomOption)
            {
                geomOption.ComputeReferences = true;
                geomOption.DetailLevel = ViewDetailLevel.Coarse;
            }
            foreach (Element ele in allSpaces)
            {
                Space s = ele as Space;
                Room r = s.Room;
                if (ForbidRoom.Contains(r.Id.IntegerValue)) continue;
                int index = roomList.FindIndex(a => a.Id == r.Id);
                GeometryElement geo = s.get_Geometry(geomOption);
                var rel = new List<XYZ>();
                XYZ start, pre = null;
                int count = 0;
                foreach (GeometryObject geomObj in geo)
                {
                    Solid geoSolid = geomObj as Solid;

                    if (geoSolid != null)
                    {
                        foreach (Edge edge in geoSolid.Edges)
                        {
                            start = edge.AsCurve().GetEndPoint(0);
                            if (r.IsPointInRoom(start))
                            {
                                if (count == 0)
                                {
                                    roomPoint[index].Add(start);
                                    count++;
                                }
                                else
                                {
                                    if (start.DistanceTo(roomPoint[index].Last<XYZ>()) > 3)
                                    {
                                        roomPoint[index].Add(start);
                                        count++;
                                    }
                                }
                            }
                        }

                    }

                }
               
            }
            return new KeyValuePair<List<Room>, List<List<XYZ>>>(roomList, roomPoint);
        }
        public TextNote AddTag(Document doc,ElementId id,string cont)
        {
            LocationPoint locPoint = doc.GetElement(id).Location as LocationPoint;
            XYZ textloc = locPoint.Point;

            ElementId defaultTextTypeId = doc.GetDefaultElementTypeId(ElementTypeGroup.TextNoteType);
            double noteWidth = .2;

            // make sure note width works for the text type
            double minWidth = TextNote.GetMinimumAllowedWidth(doc, defaultTextTypeId);
            double maxWidth = TextNote.GetMaximumAllowedWidth(doc, defaultTextTypeId);
            if (noteWidth < minWidth)
            {
                noteWidth = minWidth;
            }
            else if (noteWidth > maxWidth)
            {
                noteWidth = maxWidth;
            }

            TextNoteOptions opts = new TextNoteOptions(defaultTextTypeId);
            opts.HorizontalAlignment = HorizontalTextAlignment.Left;
            opts.Rotation = 0;

            XYZ textloc2 = new XYZ(textloc.X, textloc.Y + 2, textloc.Z);
            TextNote textNote = TextNote.Create(doc, doc.ActiveView.Id, textloc2, noteWidth,cont, opts);

            return textNote;
        }
        public IList<ElementId> CheckRamp(Document doc,UIApplication uiapp)
        {
            FilteredElementCollector col = new FilteredElementCollector(doc);
            var rampids = col.OfCategory(BuiltInCategory.OST_Ramps).WhereElementIsNotElementType().ToElementIds();
            var rel = new List<ElementId>();
            var relstr = new List<string>();
            foreach (ElementId rampid in rampids)
            {
                Element ramp = doc.GetElement(rampid);
                BoundingBoxXYZ bb = ramp.get_BoundingBox(null);
                XYZ min = bb.Min;
                XYZ max = bb.Max;
                ElementType type = doc.GetElement(ramp.GetTypeId()) as ElementType;
                Parameter p = type.get_Parameter(BuiltInParameter.RAMP_ATTR_MIN_INV_SLOPE);
                if (p.AsDouble() > RAMP_SLOPE)
                {
                    rel.Add(ramp.Id);
                    relstr.Add("Ramp is too steep!");
                }

            }
            Color(doc, rel,"R");
            WriteRel(new KeyValuePair<List<ElementId>, List<string>>(rel, relstr));
            return rel;
        }
        public IList<ElementId> CheckCorridor(Document doc)
        {
            ElementCategoryFilter spacefilter = new ElementCategoryFilter(BuiltInCategory.OST_MEPSpaces);
            FilteredElementCollector col = new FilteredElementCollector(doc);
            ICollection<ElementId> spaces = col.WherePasses(spacefilter).ToElementIds();
            var rel = new List<ElementId>();
            using (Transaction trans = new Transaction(doc))
            {
                trans.Start("Corridor");
                foreach (ElementId id in spaces)
                {
                    Element space = doc.GetElement(id);
                    if (space.Name.ToLower().Contains("corridor"))
                    {
                        BoundingBoxXYZ bb = space.get_BoundingBox(null);
                        XYZ max = bb.Max;
                        XYZ min = bb.Min;
                        double dim1 = max.Y - min.Y;
                        double dim2 = max.X - min.X;
                        double width = dim1 < dim2 ? dim1 : dim2;
                        if (width < CORR_WID)
                        {
                            string tmp = width.ToString();
                            string[] wid = tmp.Split('.');
                            string finalReport = "Cor_Wid:" + wid[0] + "." + wid[1].Substring(0, 2) + "ft";
                            rel.Add(id);
                            AddTag(doc, id, finalReport);
                        }
                        else
                            continue;
                    }
                }
                trans.Commit();
            }
            return rel;
        }

        public void Color(Document doc, List<ElementId> ids,String s)
        {
            using (Transaction trans = new Transaction(doc))
            {
                trans.Start("Highlight");
                Color color = null;
                if (s.Equals("R"))
                    color = new Color(255, 0, 0); // RGB
                else if(s.Equals("G"))
                    color = new Color(0, 255, 0); // RGB
                else
                    color = new Color(0, 0, 255); // RGB
                OverrideGraphicSettings ogs = new OverrideGraphicSettings();
                ogs.SetProjectionLineColor(color); // or other here
                foreach (ElementId id in ids)
                {

                    doc.ActiveView.SetElementOverrides(id, ogs);
                }
                trans.Commit();
            }
        }
        public void WriteRel(KeyValuePair<List<ElementId>, List<string>> rel)
        {
            string path = "D:\\AnaResult.txt";
            var str = "";
            var key = rel.Key;
            var value = rel.Value;
            for (int i = 0; i < key.Count; i++)
            {
                var id = key[i];
                var relstr = value[i];
                str += id.ToString() + "-" + relstr + "\n";
            }
            System.IO.StreamWriter file = new System.IO.StreamWriter(path,false);
            file.Write(str);
            file.Flush();
            file.Close();
            
        }
        public void RoomNameTag(Document doc)
        {
            var roomslist = GetRoomsOnLevel(doc, ViewLevel(doc));
            using (Transaction trans = new Transaction(doc))
            {
                trans.Start("RoomName");
                foreach (Room room in roomslist)
                {
                    AddTag(doc, room.Id, room.Name);
                }
                trans.Commit();
            }
        }
        public List<ElementId> BlockDoors(Document doc, List<ElementId> Doors)
        {
            var rel = new List<ElementId>();
            ElementId wallTypeId = doc.GetDefaultElementTypeId(ElementTypeGroup.WallType);// replace var with Element Id (statically-typed)
            using (Transaction trans = new Transaction(doc))
            {
                trans.Start("Block!");
                foreach (ElementId did in Doors)
                {
                    Element door = doc.GetElement(did);
                    FamilyInstance doorfam = door as FamilyInstance;
                    BoundingBoxXYZ bb = door.get_BoundingBox(null);
                    XYZ temp_a = bb.Min;
                    XYZ temp_b = bb.Max;
                    XYZ point_a = temp_a;
                    XYZ point_b = null;
                    if (Math.Abs(temp_a.X - temp_b.X) > Math.Abs(temp_a.Y - temp_b.Y))
                    {
                        if (Math.Abs(doorfam.FacingOrientation.X - 1) < 1e-6)
                        {
                            point_a = new XYZ(temp_a.X - 0.3, temp_a.Y, temp_a.Z);
                            point_b = new XYZ(temp_a.X - 0.3, temp_b.Y, temp_a.Z);
                        }
                        else
                        {
                            point_a = new XYZ(temp_b.X + 0.3, temp_a.Y, temp_a.Z);
                            point_b = new XYZ(temp_b.X + 0.3, temp_b.Y, temp_a.Z);
                        }
                    }
                    else {
                        if (Math.Abs(doorfam.FacingOrientation.Y - 1) < 1e-6)
                        {
                            point_a = new XYZ(temp_a.X, temp_a.Y-0.3, temp_a.Z);
                            point_b = new XYZ(temp_b.X, temp_a.Y-0.3, temp_a.Z);
                        }
                        else
                        {
                            point_a = new XYZ(temp_a.X, temp_b.Y+0.3, temp_a.Z);
                            point_b = new XYZ(temp_b.X, temp_b.Y+0.3, temp_a.Z);
                        }
                    }
                    Curve line = Line.CreateBound(point_a, point_b) as Curve; // Create a bound curve for function to work, a wall cannot be created with unbound line
                    Wall wall = Wall.Create(doc, line, ViewLevel(doc), false);
                    rel.Add(wall.Id);
                }
                trans.Commit();
            }
            return rel;
        }
        public void DeleteBlock(Document doc, List<ElementId> walls)
        {
            using (Transaction trans = new Transaction(doc))
            {
                trans.Start("Begin");
                doc.Delete(walls);
                trans.Commit();
            }
        }
        public List<ElementId> Colordoor(Document doc, IList<ElementId> rel)
        {
            var resDoor = new List<ElementId>();
            var adDoor = new List<ElementId>();
            var doorids = GetAllDoors(doc, ViewLevel(doc));

            foreach (ElementId did in doorids)
            {
                Element door = doc.GetElement(did);
                bool flag = false;
                FamilyInstance doorfam = door as FamilyInstance;
                Room temp1 = doorfam.FromRoom;
                Room temp2 = doorfam.ToRoom;
                foreach (ElementId id in rel)
                {
                    if (temp1 != null && temp1.Id.IntegerValue == id.IntegerValue)
                    {
                        resDoor.Add(did);
                        flag = true;
                    }
                    else if (temp2 != null && temp2.Id.IntegerValue == id.IntegerValue)
                    {
                        resDoor.Add(did);
                        flag = true;
                    }
                }
                if (flag == false)
                {
                    adDoor.Add(did);
                }
            }
            Color(doc, resDoor, "R");
            Color(doc, adDoor, "G");
            return BlockDoors(doc, resDoor);
        }
    }
}
