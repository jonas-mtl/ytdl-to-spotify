import os
from flask import Flask, Response, request, after_this_request
import core

app = Flask(__name__)

@app.route('/download')
def download():
    url = request.headers.get('url')
    if not url:
        return "Missing header: url", 500
    
    downloader = core.Downloader(url)

    if os.path.exists(downloader.mp3_file):
        def generate():
            with open(downloader.mp3_file, 'rb') as f:
                while True:
                    chunk = f.read(4096)  # Adjust chunk size as needed
                    if not chunk:
                        break
                    yield chunk

        # Create a Response object to stream the file
        response = Response(generate(), content_type='audio/mpeg')
        response.headers.set('Content-Disposition', 'attachment', filename='downloaded.mp3')

        # Register a callback to delete the file after the response is sent
        @response.call_on_close
        def on_close():
            os.remove(downloader.mp3_file)
            pass

        return response

    else:
        return "File not found", 404

@app.route('/')
def home():
    return "Running!", 200

if __name__ == '__main__':
    app.run(debug=True)