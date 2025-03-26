using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;

public class StringToListConverter : DefaultTypeConverter
{
    public override object ConvertFromString(string text, IReaderRow row, MemberMapData memberMapData)
    {
        if (string.IsNullOrWhiteSpace(text))
            return new List<string>();

        // Divide a string por vírgulas e remove espaços em branco
        var images = text.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
        var result = new List<string>();

        foreach (var img in images)
        {
            result.Add(img.Trim());
        }

        return result;
    }

    public override string ConvertToString(object value, IWriterRow row, MemberMapData memberMapData)
    {
        if (value is List<string> list)
        {
            return string.Join(",", list);
        }

        return base.ConvertToString(value, row, memberMapData);
    }
}