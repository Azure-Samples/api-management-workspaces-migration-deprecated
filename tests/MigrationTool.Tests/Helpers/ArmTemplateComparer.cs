using Microsoft.Azure.Management.ApiManagement.ArmTemplates.Common.Templates.Abstractions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MigrationTool.Tests.Helpers;

public class ArmTemplateComparer : IComparer
{
    public int Compare(object x, object y)
    {
        if (x is TemplateResource && y is TemplateResource && ((TemplateResource) x).TestEquality((TemplateResource)y)) return 0;
        return -1;
    }
}
