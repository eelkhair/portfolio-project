<html>
<head>
    <meta charset="UTF-8" />
</head>
<body style="margin: 0; padding: 0; background-color: #f1f5f9; font-family: Arial, sans-serif;">
    <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="background-color: #f1f5f9; padding: 40px 0;">
        <tr>
            <td align="center">
                <table role="presentation" width="600" cellpadding="0" cellspacing="0" style="background-color: #ffffff; border-radius: 12px; overflow: hidden; box-shadow: 0 1px 3px rgba(0,0,0,0.1);">
                    <!-- Header with logo -->
                    <tr>
                        <td style="background-color: #1e293b; padding: 24px; text-align: center;">
                            <img src="${url.resourcesUrl}/img/logo.png" alt="JobBoard" width="180" height="45" style="display: inline-block;" />
                        </td>
                    </tr>

                    <!-- Body -->
                    <tr>
                        <td style="padding: 40px 36px;">
                            <h1 style="margin: 0 0 8px; font-size: 24px; font-weight: 700; color: #1e293b;">
                                Welcome to ${(user.attributes.companyName)!"the platform"}
                            </h1>
                            <p style="margin: 0 0 24px; font-size: 15px; color: #64748b;">
                                Your account is almost ready
                            </p>

                            <p style="margin: 0 0 12px; font-size: 15px; line-height: 1.6; color: #334155;">
                                Hi ${user.firstName},
                            </p>
                            <p style="margin: 0 0 28px; font-size: 15px; line-height: 1.6; color: #334155;">
                                Please verify your email address to complete your account setup and get started.
                            </p>

                            <!-- CTA Button -->
                            <table role="presentation" cellpadding="0" cellspacing="0" style="margin: 0 0 28px;">
                                <tr>
                                    <td style="border-radius: 8px; background-color: #4f46e5;">
                                        <a href="${link}"
                                           style="display: inline-block; padding: 14px 32px; font-size: 15px; font-weight: 600; color: #ffffff; text-decoration: none; border-radius: 8px;">
                                            Verify Email Address
                                        </a>
                                    </td>
                                </tr>
                            </table>

                            <!-- Divider -->
                            <hr style="border: none; border-top: 1px solid #e2e8f0; margin: 0 0 20px;" />

                            <p style="margin: 0; font-size: 13px; line-height: 1.5; color: #94a3b8;">
                                This link will expire in ${linkExpiration} minutes. If you didn't request this, you can safely ignore this email.
                            </p>
                        </td>
                    </tr>

                    <!-- Footer -->
                    <tr>
                        <td style="background-color: #f8fafc; padding: 20px 36px; text-align: center; border-top: 1px solid #e2e8f0;">
                            <p style="margin: 0; font-size: 12px; color: #94a3b8;">
                                &copy; ${.now?string('yyyy')} JobBoard. All rights reserved.
                            </p>
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
</body>
</html>
