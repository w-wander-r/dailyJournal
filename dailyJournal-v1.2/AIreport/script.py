#v1.1
import mysql.connector
from textblob import TextBlob

def generate_ai_report(db_config):
    try:
        cnx = mysql.connector.connect(**db_config)
        cursor = cnx.cursor()

        cursor.execute("SELECT ReplyText FROM Replies")
        replies = [row[0] for row in cursor.fetchall()]

        sentiment_scores = []
        for reply in replies:
            blob = TextBlob(reply)
            sentiment_scores.append(blob.sentiment.polarity)

        average_sentiment = sum(sentiment_scores) / len(sentiment_scores)

        report = ""
        if average_sentiment > 0.2:
            report = "Overall, you seem to have had a positive week. "
        elif average_sentiment < -0.2:
            report = "It seems like you might have had a challenging week. "
        else:
            report = "Your mood this week seems to have been neutral. "

        cursor.close()
        cnx.close()

        return report

    except mysql.connector.Error as err:
        print(f"Cannot connect to DB: {err}")
        return "Error generating report..."

db_config = {
    'host': 'localhost',
    'user': 'root',
    'password': '6fbusyXH',
    'database': 'email_replies'
}

report = generate_ai_report(db_config)
print(report)
