import smtplib
import config


def send_email(toAddress, subject, msg):
    try:
        server = smtplib.SMTP('smtp.gmail.com', 587)
        server.starttls()
        server.login(config.EMAIL_ADDRESS, config.PASSWORD)

        message = 'Subject: {}    {}'.format(subject, msg)
        server.sendmail(config.EMAIL_ADDRESS, toAddress, message)
        server.quit()
        print("Success: Email sent!")

    except:
        print("Email failed to send.")


if __name__ == '__main__':
    toAddress = "john.louis.waczak@gmail.com"
    sub = "test"
    msg = "testing..."
    send_email(toAddress, sub, msg)
