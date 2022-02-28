
using System.Text.RegularExpressions;
using System.Linq;

public class Hrri
{
    public string Location;
    public string Group;
    public string Object;

    public Hrri(string strHrri)
    {
        string[] hrriElements = strHrri.Split('-');
        Location = hrriElements.Count() > 0 ? hrriElements[0] : "";
        Group = hrriElements.Count() > 1 ? hrriElements[1] : "";
        Object = hrriElements.Count() > 2 ? hrriElements[2] : "";
    }

    public static bool IsWellFormedOrigin(string hrri)
    {
        return Regex.IsMatch(hrri, "[A-Z0-9]+-[A-Z0-9]+-[A-Z0-9]+(-[A-Z0-9]+)?");
    }

    public override string ToString()
    {
        return string.Format("{0}-{1}-{2}", Location, Group, Object);
    }
}