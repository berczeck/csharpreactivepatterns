using System;

namespace Correlation_Identifier
{
    class Program
    {
        static void Main(string[] args)
        {
        }
    }

    public class PriceQuote { public Guid Identifier { get; set; } }
    public class RequestPriceQuote { public Guid Identifier { get; set; } }
    public class PriceQuoteTimedOut { public Guid Identifier { get; set; } }
    public class RequiredPriceQuotesForFulfillment { public Guid Identifier { get; set; } }
    public class QuotationFulfillment { public Guid Identifier { get; set; } }
    public class BestPriceQuotation { public Guid Identifier { get; set; } }
}
