"""Rebuild the three report diagrams. Requires Pillow; all outputs stay beside this file."""
from pathlib import Path
import math
from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parent
S = 2
FONT = Path('C:/Windows/Fonts/arial.ttf')
BOLD = Path('C:/Windows/Fonts/arialbd.ttf')
INK, BLUE, MUTED, EDGE = '#18242F', '#1F547A', '#526271', '#8799A8'

class Diagram:
    def __init__(self, width, height):
        self.image = Image.new('RGB', (width*S, height*S), 'white')
        self.draw = ImageDraw.Draw(self.image)
    def text(self, x, y, value, size=24, bold=False, color=INK, anchor='mm'):
        font = ImageFont.truetype(str(BOLD if bold else FONT), size*S)
        self.draw.multiline_text((x*S, y*S), value, font=font, fill=color, anchor=anchor,
                                 spacing=7*S, align='left' if anchor != 'mm' else 'center')
    def box(self, x, y, w, h, title, body='', fill='#F2F6F9', size=28, body_size=23):
        self.draw.rounded_rectangle((x*S,y*S,(x+w)*S,(y+h)*S),radius=10*S,
                                    fill=fill,outline=EDGE,width=2*S)
        self.text(x+w/2,y+(h/2 if not body else 27),title,size,True)
        if body:self.text(x+w/2,y+64,body,body_size)
    def line(self,x,y,x2,y2,color='#CBD5DE',width=1):
        self.draw.line((x*S,y*S,x2*S,y2*S),fill=color,width=width*S)
    def arrow(self, points, dashed=False, color=BLUE, width=3):
        points=[(x*S,y*S) for x,y in points]
        for p,q in zip(points,points[1:]):
            length=math.dist(p,q)
            if dashed:
                for a in range(0,int(length),16*S):
                    b=min(a+8*S,length)
                    self.draw.line([tuple(p[i]+(q[i]-p[i])*a/length for i in (0,1)),
                                    tuple(p[i]+(q[i]-p[i])*b/length for i in (0,1))],fill=color,width=width*S)
            else:self.draw.line([p,q],fill=color,width=width*S)
        p,q=points[-2:]; angle=math.atan2(q[1]-p[1],q[0]-p[0]); length=11*S
        self.draw.polygon([q,(q[0]-length*math.cos(angle-.45),q[1]-length*math.sin(angle-.45)),
                             (q[0]-length*math.cos(angle+.45),q[1]-length*math.sin(angle+.45))],fill=color)
    def save(self,name):self.image.save(ROOT/name)

def dependencies():
    d=Diagram(1200,850)
    d.text(600,28,'Application project dependencies',31,True)
    d.draw.rounded_rectangle((35*S,60*S,1165*S,690*S),radius=12*S,outline=EDGE,width=2*S)
    d.text(600,82,'One deployed ASP.NET Core process',22,color=MUTED)
    nodes=[(115,'OnlineStore','Web host / controllers / Razor / middleware','#EAF1F8'),
           (260,'OnlineStore.Business','Services / interfaces / shipping policy','#EAF4EF'),
           (405,'OnlineStore.DataAccess','DbContext / migrations / initialization','#FFF4DF'),
           (550,'OnlineStore.Models','Entities / view models / shared resources','#F0EDF7')]
    for y,title,body,fill in nodes:d.box(310,y,580,95,title,body,fill)
    for a,b in [(210,260),(355,405),(500,550)]:d.arrow([(600,a),(600,b)])
    d.arrow([(310,160),(125,160),(125,597),(310,597)])
    d.text(142,380,'Web also\nreferences\nModels',22,color=MUTED,anchor='lm')
    d.arrow([(890,160),(1085,160),(1085,452),(890,452)])
    d.text(990,215,'Web to\nDataAccess',21,color=MUTED)
    d.arrow([(890,307),(965,307),(965,598),(890,598)])
    d.text(995,540,'Business\nto Models',21,color=MUTED,anchor='lm')
    d.text(600,668,'Arrows show direct ProjectReference dependencies; none point back to Web.',20,color=MUTED)
    d.box(70,722,1060,94,'OnlineStore.Tests','References all four application projects; runs separately from deployment.',size=27)
    d.save('architecture-dependencies.png')

def mvc():
    d=Diagram(1200,690)
    d.box(25,35,240,100,'Browser','HTTP / user input')
    d.box(410,35,360,100,'Controller','ProductsController.Index','#EAF1F8')
    d.box(905,35,270,125,'Model / service','IProductService\nProductService','#EAF4EF',body_size=22)
    d.box(905,290,270,125,'Data access','ApplicationDbContext\nSQL Server','#FFF4DF',body_size=22)
    d.box(410,480,360,125,'View','Razor renders\nProductListViewModel','#EAF1F8',body_size=22)
    d.arrow([(265,80),(410,80)]);d.text(335,40,'1. GET',23,color=MUTED)
    d.arrow([(770,80),(905,80)]);d.text(838,40,'2. Call',23,color=MUTED)
    d.arrow([(1040,160),(1040,290)]);d.text(1055,225,'3. EF query',22,color=MUTED,anchor='lm')
    d.arrow([(905,140),(830,140),(830,245),(690,245),(690,135)],dashed=True)
    d.text(700,320,'4. Return view model\n(page / filters)',23,color=MUTED)
    d.arrow([(520,135),(520,480)]);d.text(605,405,'5. View(vm)',23,color=MUTED)
    d.arrow([(410,542),(145,542),(145,135)],dashed=True);d.text(250,510,'6. HTML',23,color=MUTED)
    d.text(600,645,'Routing and middleware run before the controller. Views never receive HTTP directly.',22,color=MUTED)
    d.save('architecture-mvc.png')

def sequence():
    d=Diagram(1200,1260)
    xs=[100,345,590,835,1100]
    for x,title in zip(xs,['Browser','Pipeline','Controller','Service','SQL Server']):
        d.box(x-95,15,190,65,title,size=25)
        d.line(x,80,x,1190)
    def event(a,b,y,label,dashed=False,color=BLUE):
        d.arrow([(a,y),(b,y)],dashed,color)
        d.text((a+b)/2,y-18,label,21,color=color)
    def band(y,text):
        d.draw.rectangle((5*S,y*S,1195*S,(y+38)*S),fill='#EDF2F6')
        d.text(20,y+19,text,23,True,anchor='lm')
    band(95,'A  Successful catalogue read')
    event(100,345,172,'GET /Products')
    event(345,590,218,'Route / authorize')
    event(590,835,264,'GetPagedAsync')
    event(835,1100,310,'EF Core query')
    event(1100,835,356,'Rows',True)
    event(835,590,402,'View model',True)
    event(590,100,448,'Razor renders HTML',True)
    band(480,'B  Dependency unavailable and later recovery')
    event(100,835,552,'Later GET through the same pipeline and controller')
    event(835,1100,598,'Database access fails',color='#9A4D23')
    event(835,345,644,'Transient DB failure propagates',True,'#9A4D23')
    event(345,100,690,'503 + safe HTML / JSON',True,'#9A4D23')
    d.text(630,724,'Retry-After: 5; check order status before resubmitting a write.',21,color=MUTED)
    event(100,835,770,'After database recovery: a later GET can succeed')
    band(808,'C  Order write transaction safety')
    event(100,835,880,'POST checkout through authentication and validation')
    event(835,1100,930,'Begin Serializable')
    event(835,1100,980,'Recheck / save',color=BLUE)
    event(835,1100,1030,'COMMIT or ROLLBACK')
    event(835,100,1080,'Result / validation feedback; DB failure follows B',True)
    d.text(600,1133,'No automatic replay of POST. An uncertain response does not prove no commit occurred.',22,color=MUTED)
    d.text(600,1216,'Health: /health/live = process only; /health/ready and /health = database probe.',21,color=MUTED)
    d.save('architecture-sequence.png')

if __name__=='__main__':
    dependencies(); mvc(); sequence()
    print('Generated 3 report diagrams.')
