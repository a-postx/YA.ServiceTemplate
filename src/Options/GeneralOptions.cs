namespace YA.ServiceTemplate.Options;

// оценка целесообразности: нужно ли брать дефолтные настройки из файла,
// a к ним прикрутить настройки из Амазона (с приоритетом)
public class GeneralOptions
{
    public string CorrelationIdHeader { get; set; }
    public int MaxLogFieldLength { get; set; }
    public int DefaultPaginationPageSize { get; set; }
}
