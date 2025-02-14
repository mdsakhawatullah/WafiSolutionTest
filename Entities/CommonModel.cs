namespace Wafi.SampleTest.Entities
{
    public class CommonModel
    {
        #region audit
        public DateTime CreationTime { get; set; }
        public DateTime? LastModificationTime { get; set; }
        public DateTime? DeletionTime { get; set; }
        #endregion
    }
}
