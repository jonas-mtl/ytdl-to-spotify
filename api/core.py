from mutagen.id3 import ID3, APIC, TIT2, TPE1, TYER
from mutagen.mp3 import MP3
from moviepy.editor import *
from PIL import Image

import os
import requests
import uuid
import pytube

class Downloader :
    uuid = ""
    mp3_file = ""
    yt = None

    def __init__(self, url):
        self.uuid = str(uuid.uuid4())

        self.downloadMp3(url)
        self.downloadThumbnail()
        self.addMetadata()

    def downloadMp3(self, url):
        self.yt = pytube.YouTube(url)
        stream = self.yt.streams.filter(only_audio=True).first()
        output_file = stream.download()
        
        base, ext = os.path.splitext(output_file)
        self.mp3_file = base + ".mp3"

        audio_clip = AudioFileClip(output_file)
        audio_clip.write_audiofile(self.mp3_file)
        audio_clip.close()

        os.remove(output_file)

    def downloadThumbnail(self):
        thumbnail_response = requests.get(self.yt.thumbnail_url)
        thumbnail_file = self.uuid + '.jpg'

        with open(thumbnail_file, 'wb') as f:
            f.write(thumbnail_response.content)

        thumbnail_img = Image.open(thumbnail_file)
        width, height = thumbnail_img.size

        if width > height:
            left = (width - height) / 2
            top = 0
            right = left + height
            bottom = height
        else:
            left = 0
            top = (height - width) / 3  
            right = width
            bottom = top + width

        thumbnail_img = thumbnail_img.crop((left, top, right, bottom))
        thumbnail_img.save(thumbnail_file)
        thumbnail_img.close()

    def addMetadata(self):
        audio = MP3(self.mp3_file, ID3=ID3)

        if len(self.yt.title.split("-")) == 2:
            title = self.yt.title.split("-")[1]
            author = self.yt.title.split("-")[0]
        else:
            title = self.yt.title
            author = self.yt.author

        publish_date = self.yt.publish_date.strftime("%Y")

        audio.tags.add(TPE1(encoding=3, text=author))  # Artist
        audio.tags.add(TIT2(encoding=3, text=title))  # Title
        audio.tags.add(TYER(encoding=3, text=publish_date))  # Year

        with open(self.uuid+".jpg", 'rb') as albumart:
            audio.tags.add(
                APIC(
                    encoding=3,  # UTF-8
                    mime='image/jpeg',  
                    type=3, 
                    desc='Cover',  
                    data=albumart.read()  
                )
            )

        audio.save()
        os.remove(self.uuid+".jpg")

    def getMp3(self):
        print(self.mp3_file)
        return self.mp3_file

