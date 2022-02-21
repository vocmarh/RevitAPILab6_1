using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using Prism.Commands;
using RevitAPILibrary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WallUtils = RevitAPILibrary.WallUtils;



namespace RevitAPILab6_1
{
    public class MainViewViewModel
    {
        private ExternalCommandData _commandData;

        public List<DuctType> DuctTypes { get; } = new List<DuctType>();
        public List<Level> Levels { get; } = new List<Level>();
        public DelegateCommand SaveCommand { get; }
        public double DuctOffset { get; set; }
        public List<XYZ> Points { get; } = new List<XYZ>();
        public MEPSystemType DuctSystemType { get; }
        public DuctType SelectedDuctType { get; set; }
        public Level SelectedLevel { get; set; }
        public MainViewViewModel(ExternalCommandData commandData)
        {
            _commandData = commandData;
            DuctTypes = DuctUtils.GetDuctTypes(commandData);
            Levels = LevelsUtils.GetLevels(commandData);
            SaveCommand = new DelegateCommand(OnSaveCommand);
            DuctOffset = 0;
            Points = SelectionUtils.GetPoints(_commandData, "Выберите точки", ObjectSnapTypes.Endpoints);
        }

        private void OnSaveCommand()
        {
            UIApplication uiapp = _commandData.Application;
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Document doc = uidoc.Document;

            if (Points.Count < 2 || SelectedDuctType == null || SelectedLevel == null)
                return;
            var curves = new List<Curve>();
            for (int i = 0; i < Points.Count; i++)
            {
                if (i == 0)
                    continue;
                var prevPoint = Points[i - 1];
                var currentPoint = Points[i];
                Curve curve = Line.CreateBound(prevPoint, currentPoint);
                curves.Add(curve);
            }

            MEPSystemType mepSystemType = new FilteredElementCollector(doc)
                .OfClass(typeof(MEPSystemType))
                .Cast<MEPSystemType>()
                .FirstOrDefault(sysType => sysType.SystemClassification == MEPSystemClassification.SupplyAir);

            using (var ts = new Transaction(doc, "Create duct"))
            {
                ts.Start();
                foreach (var curve in curves)
                {
                    Duct duct = Duct.Create(doc, mepSystemType.Id, SelectedDuctType.Id, SelectedLevel.Id, curve.GetEndPoint(0), curve.GetEndPoint(1));
                    Parameter ductOffset = duct.get_Parameter(BuiltInParameter.RBS_OFFSET_PARAM);
                    ductOffset.Set(UnitUtils.ConvertToInternalUnits(DuctOffset, UnitTypeId.Millimeters));
                }
                ts.Commit();
            }
            RaiseCloseRequest();
        }

        public event EventHandler CloseRequest;
        private void RaiseCloseRequest()
        {
            CloseRequest?.Invoke(this, EventArgs.Empty);
        }
    }
}