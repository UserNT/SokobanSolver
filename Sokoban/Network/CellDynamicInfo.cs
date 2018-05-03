namespace Sokoban.Network
{
    public class CellDynamicInfo
    {
        public int AreaId { get; set; }

        public bool HoldsKeeper { get; set; }

        public bool HoldsBox { get; set; }

        public int StepsToKeeper { get; set; }

        public override string ToString()
        {
            return string.Format("Area: {0}; Steps: {1}; {2}", AreaId, StepsToKeeper, HoldsKeeper ? "Keeper" : HoldsBox ? "Box" : "");
        }
    }
}
