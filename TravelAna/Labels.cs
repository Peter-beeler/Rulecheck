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
    public class Labels : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            //Get application and documnet objects
            UIApplication uiapp = commandData.Application;
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Document doc = uiapp.ActiveUIDocument.Document;            
            
            Report(doc);

            return Result.Succeeded;
        }
        private void Report(Document doc)
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
            using (Transaction trans = new Transaction(doc))
            {
                trans.Start("Labels");
                // Find all Room instances in the document by using category filter
                ElementCategoryFilter filter = new ElementCategoryFilter(BuiltInCategory.OST_Rooms);

                // Apply the filter to the elements in the active document
                // Use shortcut WhereElementIsNotElementType() to find wall instances only
                IEnumerable<Room> rooms = new FilteredElementCollector(doc)
                                                          .WhereElementIsNotElementType()
                                                          .OfClass(typeof(SpatialElement))
                                                          .Where(e => e.GetType() == typeof(Room))
                                                          .Where(e => e.LevelId.IntegerValue == levelId.IntegerValue )
                                                          .Cast<Room>();

                
                foreach (Element e in rooms)
                {
                    Room room = e as Room;
                    LocationPoint locPoint = doc.GetElement(e.Id).Location as LocationPoint;
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


                    TextNote textNote = TextNote.Create(doc, doc.ActiveView.Id, textloc, noteWidth, room.Name, opts);
                }                
                trans.Commit();
            }
        }
        
    }
}
