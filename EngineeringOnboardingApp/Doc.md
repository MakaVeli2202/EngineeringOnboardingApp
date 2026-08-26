# Software Engineering Onboarding Guide

## Before You Start

After receiving your laptop, complete the initial PC setup by following the instructions provided through the QR code.

### TODO
- Create D: partition for databases and archives.

---

## 1. Test Your VPN Connection

Try connecting to **GlobalProtect VPN**.

- If the connection is successful, continue to **Section 3**.
- If you receive an **"Authentication Failed"** error or are unable to connect, continue with **Section 2**.

---

## 2. GlobalProtect Troubleshooting

### Request an E5 License

An E5 license is required for GlobalProtect VPN access. Without it, you may receive authentication or authorization errors.

Submit a request for an E5 license through the appropriate IT support process.

### Use Prisma Browser in the Meantime

While waiting for the E5 license, install and use Prisma Browser.

Follow the guide below:

[Prisma Browser Installation Guide](https://pulse.service-now.com/mth?id=kb_article&sysparm_article=KB00308598)

After installation, use Prisma Browser to access GE HealthCare applications.

### Access Issues

If an application is unavailable or access is denied, you will need to request access through **MyAccess**.

### Example: Requesting Box Access

1. Go to `myaccess.gehealthcare.com`
2. Select **Start an Access Request**
3. Select **Application**
4. Search for **BOX**
5. Select **Add**
6. Select **Next**
7. Review the request and select **Submit**

### Example: Requesting Confluence Access

Try accessing:

[GE HealthCare Confluence](https://ge-hc.atlassian.net/wiki/home)

If access is unavailable:

1. Open: [Confluence Access Portal](https://edh.apps.ge-healthcare.net/confluence/)
2. Select **Account Creation / Reactivation**
3. Search for **Confluence Global SaaS**
4. Complete and submit the request
5. No attachment is required

While waiting for access approvals, continue with the assigned learning modules described in **Section 4**.

---

## 3. Bookmark Important Links

If GlobalProtect is working correctly, bookmark the following sites in your preferred browser (Edge or Chrome).

### Box
Cloud storage and file sharing.

### Confluence
Internal documentation and knowledge base.

### CoreData
Attendance and time management.

### Learner Dashboard (Emerge U)
Training materials and onboarding courses.

### MyAccess
Access requests for software, applications, and permissions such as:

- Box
- GitLab
- GitHub Copilot
- Other internal applications

---

## 4. Complete Assigned Learning

Before diving into project work, complete the onboarding and compliance training assigned through **Emerge U**.

This is a good use of time while waiting for:

- VPN access
- Software licenses
- Application permissions
- Team-specific access requests

Depending on approval times, obtaining all required access may take several days up to one week.

Starting the training early helps make productive use of this waiting period.

---

## 5. Install and Configure Development Tools

Install and configure the required development tools.

Typical tools include:

- Git
- Git Extensions
- GitLab
- Git Bash
- Visual Studio 2022 Professional

### Git Bash

When following the Git setup guide, ignore the SSH configuration section if instructed by your team.

Start from the relevant setup steps provided by your team documentation.

### Visual Studio

- Install the **Professional Edition** and import the required team configuration settings.

  ```
  VS_2026_Configuration.vsconfig
  ```

- If only installing Visual Studio 2026, also install:

  ```
  dotnet-sdk-10.0.109-win-x64.exe
  winsdksetup.exe
  ```

- Additional configuration steps:
  - TBD

---

## Additional Resources

### Email Administration

For special email group requests or distribution list access, first discuss the requirement with **Simon** before submitting a request.

### Voluson Valley Info

Useful source for GE HealthCare news, internal articles, updates, and organization-specific information.

---

## Notes

- Access requirements may vary depending on your role and team.
- Software Engineering roles typically require additional tools and permissions such as GitLab, VPN access, and development environments.
- If you are unsure whether you need a specific application or permission, ask your team lead or onboarding buddy before raising a request.

Links
## Important Links

### CoreData
Attendance and time management.

https://gemed-timetravel.coredat.com/

### GitLab
Source control, code reviews, and CI/CD.

https://gitlab.apps.ge-healthcare.net/

### MyAccess
Request access to applications, permissions, and software.

https://gehealthcare.saviyntcloud.com/ECMv6/request/requestHome

### Emerge U Learning
Training, onboarding, and compliance courses.

https://gehealthcare.sumtotal.host/

### Pulse
IT knowledge base, support articles, and internal resources.

https://pulse.service-now.com/

### Prisma Browser Guide
Instructions for installing and using Prisma Browser.

https://pulse.service-now.com/mth?id=kb_article&sysparm_article=KB00308598
