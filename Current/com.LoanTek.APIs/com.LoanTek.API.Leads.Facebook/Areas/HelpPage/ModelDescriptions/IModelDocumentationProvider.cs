using System;
using System.Reflection;

namespace com.LoanTek.API.Leads.Facebook.Areas.HelpPage.ModelDescriptions
{
    public interface IModelDocumentationProvider
    {
        string GetDocumentation(MemberInfo member);

        string GetDocumentation(Type type);
    }
}