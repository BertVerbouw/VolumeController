package com.verbouw.bert.volumecontroller;

import android.os.Handler;
import android.support.v7.app.AppCompatActivity;
import android.os.Bundle;
import android.widget.TextView;

import com.android.volley.Request;
import com.android.volley.RequestQueue;
import com.android.volley.Response;
import com.android.volley.VolleyError;
import com.android.volley.toolbox.StringRequest;
import com.android.volley.toolbox.Volley;

public class MainActivity extends AppCompatActivity {
    private Handler handler;
    private RequestQueue queue;
    private int interval = 1500;

    public MainActivity() {
        queue = Volley.newRequestQueue(this);
    }

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        setContentView(R.layout.activity_main);
        handler = new Handler();
    }

    @Override
    protected void onResume(){
        super.onResume();
        apiLoop.run();
    }

    @Override
    public void onStop(){
        super.onStop();
        handler.removeCallbacks(apiLoop);
    }

    Runnable apiLoop = new Runnable() {
        @Override
        public void run() {
            try {
                sendGetRequest(); //this function can change value of interval.
            } finally {
                // 100% guarantee that this always happens, even if
                // your update method throws an exception
                handler.postDelayed(apiLoop, interval);
            }
        }
    };

    private void sendGetRequest(){
        // Instantiate the RequestQueue.
        String url ="http://localhost:8081/get";

        // Request a string response from the provided URL.
        StringRequest stringRequest = new StringRequest(Request.Method.GET, url,
                new Response.Listener<String>() {
                    @Override
                    public void onResponse(String response) {
                    }
                }, new Response.ErrorListener() {
            @Override
            public void onErrorResponse(VolleyError error) {
            }
        });
        // Add the request to the RequestQueue.
        queue.add(stringRequest);
    }

}
