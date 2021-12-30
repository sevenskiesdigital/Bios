namespace Bios
{
    public class FaceRectangle
    {
        public int width { get; set; }
        public int height { get; set; }
        public int left { get; set; }
        public int top { get; set; }
    }
    public class Face
    {
        public string faceId { get; set; }
        public string recognitionModel { get; set; }
        public FaceRectangle faceRectangle { get; set; }
    }
}
