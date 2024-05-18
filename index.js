import express from 'express';
import { spawn } from 'child_process';
import fs from 'fs';

const app = express();
const PORT = process.env.PORT || 3000;

app.get('/', async (req, res) => {
  res.status(200).send('Running!');
});

app.get('/download', async (req, res) => {
  const videoUrl = req.headers['videourl'] || 'https://www.youtube.com/watch?v=MlimN-xNLe4';

  const child = spawn('bin/yt2mp3/yt2mp3.exe', [videoUrl]);
  let title = undefined;

  child.stdout.on('data', (data) => {
    process.stdout.write(data);

    if (data.toString().includes('- Downloading... |')) {
      title = data.toString().split(' - Downloading... | ')[1].replaceAll('\r\n', '');
    }
  });

  child.stderr.on('data', (data) => {
    console.error(`Error: ${data}`);
    return res.status(400).send('Something went wrong!');
  });

  child.on('close', (code) => {
    res.download(`./${title}.mp3`, `${title}.mp3`, (err) => {
      if (err) {
        console.log(err);
        return res.status(500).send('File could not be downloaded!');
      }

      fs.unlinkSync(`${title}.mp3`);
      return res.status(200).send('File successfully downloaded!');
    });
  });
});

app.listen(PORT, () => {
  console.log(`Server is running on port ${PORT}`);
});
